using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Implements and manages the lifecycle of the network layer.
    /// </summary>
    public class GrpcNetworkServer : IAElfNetworkServer, ISingletonDependency
    {
        private ChainOptions ChainOptions => ChainOptionsSnapshot.Value;
        public IOptionsSnapshot<ChainOptions> ChainOptionsSnapshot { get; set; }
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly IGrpcServerContext _serverContext;
        private readonly IPeerDialer _peerDialer;
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly IPeerPool _peerPool;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }

        public GrpcNetworkServer(IGrpcServerContext serverContext, IPeerPool peerPool, 
             IPeerDialer peerDialer, IHandshakeProvider handshakeProvider)
        {
            _serverContext = serverContext;
            _peerDialer = peerDialer;
            _handshakeProvider = handshakeProvider;
            _peerPool = peerPool;

            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task StartAsync()
        {
            await StartListeningAsync();
            await DialBootNodesAsync();

            await EventBus.PublishAsync(new NetworkInitializedEvent());
        }
        
        /// <summary>
        /// Starts gRPC's server by binding the peer services, sets options and adds interceptors.
        /// </summary>
        internal async Task StartListeningAsync()
        {
            _serverContext.RegisterConnectionCallback(OnConnectionStarted);
            _serverContext.RegisterHandshakeCallback(OnHandshakeStarted);

            // start listening
            await _serverContext.StartServerAsync();
        }

        private async Task<ConnectReply> OnConnectionStarted(string peerConnectionIp, ConnectionInfo peerConnectionInfo)
        {
            // TODO limit the amount of connections per host and number of peers "connecting"
            var peer = GrpcUrl.Parse(peerConnectionIp);
            
            if (peer == null)
                return new ConnectReply { Error = ConnectError.InvalidPeer };

            var error = ValidateConnectionInfo(peerConnectionInfo);
            if (error != ConnectError.ConnectOk)
                return new ConnectReply { Error = error };
            
            string pubKey = peerConnectionInfo.Pubkey.ToHex();
            
            var currentPeer = _peerPool.FindPeerByPublicKey(pubKey);
            if (currentPeer != null)
            {
                Logger.LogWarning($"Cleaning up {currentPeer} already known.");
                return new ConnectReply { Error = ConnectError.ConnectionRefused };
            }

            // TODO: find a URI type to use
            var peerAddress = peer.IpAddress + ":" + peerConnectionInfo.ListeningPort;
            
            Logger.LogDebug($"Attempting to create channel to {peerAddress}");
            var grpcPeer = await _peerDialer.DialBackPeer(peerAddress, peerConnectionInfo);

            // If auth ok -> add it to our peers
            if (!_peerPool.TryAddPeer(grpcPeer))
            {
                Logger.LogWarning($"Stopping connection, peer already in the pool {grpcPeer.Info.Pubkey}.");
                await grpcPeer.DisconnectAsync(false);
            }
            
            Logger.LogDebug($"Added to pool {grpcPeer.Info.Pubkey}.");

            var connectInfo = await _serverContext.GetConnectionInfoAsync();
            return new ConnectReply { Info = connectInfo};
        }
        
        private async Task<HandshakeReply> OnHandshakeStarted(string peerId, Handshake handshake)
        {
            var error = ValidateHandshake(handshake, peerId);

            if (error != HandshakeError.HandshakeOk)
            {
                Logger.LogWarning($"Handshake not valid: {error}");
                return new HandshakeReply { Error = error };
            }
            
            var peer = _peerPool.FindPeerByPublicKey(peerId) as GrpcPeer;

            // should never happen because the interceptor takes care of this, but if the peer
            // is remove between the interceptor's check and here: stop the process.
            if (peer == null)
            {
                Logger.LogWarning($"Peer {peerId}: {error}");
                return new HandshakeReply { Error = HandshakeError.WrongConnection };
            }
            
            peer.UpdateLastReceivedHandshake(handshake);
            
            Logger.LogTrace($"Connected to {peer} - LIB height {peer.LastKnownLibHeight}, " +
                            $"best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}].");
            
            return new HandshakeReply { Handshake = await _handshakeProvider.GetHandshakeAsync() };
        }

        private ConnectError ValidateConnectionInfo(ConnectionInfo connectionInfo)
        {
            // verify chain id
            if (connectionInfo.ChainId != ChainOptions.ChainId)
                return ConnectError.ChainMismatch;

            // verify protocol
            if (connectionInfo.Version != KernelConstants.ProtocolVersion)
                return ConnectError.ProtocolMismatch;
            
            // verify if we still have room for more peers
            if (NetworkOptions.MaxPeers != 0 && _peerPool.IsFull())
            {
                Logger.LogWarning($"Cannot add peer, there's currently {_peerPool.PeerCount} peers (max. {NetworkOptions.MaxPeers}).");
                return ConnectError.ConnectionRefused;
            }

            return ConnectError.ConnectOk;
        }

        private HandshakeError ValidateHandshake(Handshake handshake, string connectionPubkey)
        {
            if (handshake?.HandshakeData == null)
                return HandshakeError.InvalidHandshake;

            if (handshake.HandshakeData.Pubkey.ToHex() != connectionPubkey)
                return HandshakeError.InvalidKey;
            
            var validData = CryptoHelper.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HandshakeData).ToByteArray(), handshake.HandshakeData.Pubkey.ToByteArray());
            
            if (!validData)
                return HandshakeError.WrongSignature;
            
            // verify authentication
            var pubKey = handshake.HandshakeData.Pubkey.ToHex();
            
            if (NetworkOptions.AuthorizedPeers == AuthorizedPeers.Authorized 
                && !NetworkOptions.AuthorizedKeys.Contains(pubKey))
            {
                Logger.LogDebug($"{pubKey} not in the authorized peers.");
                return HandshakeError.NotListed;
            }

            return HandshakeError.HandshakeOk;
        }

        /// <summary>
        /// Connects to the boot nodes provided in the network options.
        /// </summary>
        private async Task DialBootNodesAsync()
        {
            if (NetworkOptions.BootNodes == null || !NetworkOptions.BootNodes.Any())
            {
                Logger.LogWarning("Boot nodes list is empty.");
                return;
            }

            var taskList = NetworkOptions.BootNodes.Select(ConnectAsync).ToList();
            await Task.WhenAll(taskList.ToArray<Task>());
        }
        
        /// <summary>
        /// Connects to a node with the given ip address and adds it to the node's peer pool.
        /// </summary>
        /// <param name="ipAddress">the ip address of the distant node</param>
        /// <returns>True if the connection was successful, false otherwise</returns>
        public async Task<bool> ConnectAsync(string ipAddress)
        {
            Logger.LogTrace($"Attempting to reach {ipAddress}.");

            if (_peerPool.FindPeerByAddress(ipAddress) != null)
            {
                Logger.LogWarning($"Peer {ipAddress} is already in the pool.");
                return false;
            }

            GrpcPeer peer;
            
            try
            {
                // create the connection to the distant node
                peer = await _peerDialer.DialPeerAsync(ipAddress, await _serverContext.GetConnectionInfoAsync());
            }
            catch (PeerDialException ex)
            {
                Logger.LogError(ex, $"Dial exception {ipAddress}:");
                return false;
            }
            
            var peerPubkey = peer.Info.Pubkey;

            if (!_peerPool.TryAddPeer(peer))
            {
                Logger.LogWarning($"Peer {peerPubkey} is already in the pool.");
                await peer.DisconnectAsync(false);
                return false;
            }
            
            Handshake peerHandshake;
            
            try
            {
                peerHandshake = await peer.DoHandshakeAsync(await _handshakeProvider.GetHandshakeAsync());
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Handshake failed to {ipAddress} - {peerPubkey}.");
                await DisconnectAsync(peer);
                return false;
            }

            HandshakeError handshakeError = ValidateHandshake(peerHandshake, peerPubkey);
            if (handshakeError != HandshakeError.HandshakeOk)
            {
                Logger.LogWarning($"Invalid handshake [{handshakeError}] from {ipAddress} - {peerPubkey}");
                await DisconnectAsync(peer);
                return false;
            }
            
            Logger.LogTrace($"Connected to {peer} - LIB height {peer.LastKnownLibHeight}, " +
                            $"best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}].");
            
            FireConnectionEvent(peer);

            return true;
        }

        public async Task DisconnectAsync(IPeer peer, bool sendDisconnect = false)
        {
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));
            
            // clean the pool
            if (_peerPool.RemovePeer(peer.Info.Pubkey) == null)
                Logger.LogWarning($"{peer} was not found in pool.");
            
            // clean the peer
            await peer.DisconnectAsync(sendDisconnect);
            
            Logger.LogDebug($"Removed peer {peer}");
        }

        private void FireConnectionEvent(GrpcPeer peer)
        {
            var nodeInfo = new NodeInfo { Endpoint = peer.IpAddress, Pubkey = peer.Info.Pubkey.ToByteString() };
            var bestChainHeader = peer.LastReceivedHandshake.HandshakeData.BestChainHead;

            _ = EventBus.PublishAsync(new PeerConnectedEventData(nodeInfo, bestChainHeader));
        }

        public async Task StopAsync(bool gracefulDisconnect = true)
        {
            try
            {
                await _serverContext.StopServerAsync();
            }
            catch (InvalidOperationException)
            {
                // if server already shutdown, we continue and clear the channels.
            }

            var peers = _peerPool.GetPeers(true);
            foreach (var peer in peers)
            {
                // todo Task.WhenAll + timeout + disc msg
                await peer.DisconnectAsync(false);
            }
        }

        public void Dispose()
        {
            // TODO: implement dispose pattern
        }
    }
}