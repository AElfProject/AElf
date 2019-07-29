using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc.Connection
{
    public class ConnectionService : IConnectionService
    {
        private ChainOptions ChainOptions => ChainOptionsSnapshot.Value;
        public IOptionsSnapshot<ChainOptions> ChainOptionsSnapshot { get; set; }
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IPeerPool _peerPool;
        private readonly IPeerDialer _peerDialer;
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly IConnectionInfoProvider _connectionInfoProvider;
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        public ConnectionService(IPeerPool peerPool, IPeerDialer peerDialer, 
            IHandshakeProvider handshakeProvider, IConnectionInfoProvider connectionInfoProvider)
        {
            _peerPool = peerPool;
            _peerDialer = peerDialer;
            _handshakeProvider = handshakeProvider;
            _connectionInfoProvider = connectionInfoProvider;

            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
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

        public GrpcPeer GetPeerByPubkey(string pubkey)
        {
            return _peerPool.FindPeerByPublicKey(pubkey) as GrpcPeer;
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
                peer = await _peerDialer.DialPeerAsync(ipAddress);
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
                
        private void FireConnectionEvent(GrpcPeer peer)
        {
            var nodeInfo = new NodeInfo { Endpoint = peer.IpAddress, Pubkey = peer.Info.Pubkey.ToByteString() };
            var bestChainHeader = peer.LastReceivedHandshake.HandshakeData.BestChainHead;

            _ = EventBus.PublishAsync(new PeerConnectedEventData(nodeInfo, bestChainHeader));
        }
                
        public async Task<ConnectReply> DialBackAsync(string peerConnectionIp, ConnectionInfo peerConnectionInfo)
        {
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

            var connectInfo = await _connectionInfoProvider.GetConnectionInfoAsync();
            return new ConnectReply { Info = connectInfo};
        }
        
        public async Task<HandshakeReply> CheckIncomingHandshakeAsync(string peerId, Handshake handshake)
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

        public async Task DisconnectPeersAsync(bool gracefulDisconnect)
        {
            var peers = _peerPool.GetPeers(true);
            foreach (var peer in peers)
            {
                await peer.DisconnectAsync(gracefulDisconnect);
            }
        }
        
        public void RemovePeer(string pubkey)
        {
            _peerPool.RemovePeer(pubkey);
        }
    }
}