using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
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
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly PeerService.PeerServiceBase _serverService;
        private readonly AuthInterceptor _authInterceptor;
        private readonly IPeerDialer _peerDialer;
        private readonly IHandshakeProvider _handshakeProvider;
        
        private readonly IPeerPool _peerPool;

        private Server _server;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }

        public GrpcNetworkServer(PeerService.PeerServiceBase serverService, IPeerPool peerPool, 
            AuthInterceptor authInterceptor, IPeerDialer peerDialer, IHandshakeProvider handshakeProvider)
        {
            _serverService = serverService;
            _authInterceptor = authInterceptor;
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

            await EventBus.PublishAsync(new NetworkInitializationFinishedEvent());
        }

        /// <summary>
        /// Starts gRPC's server by binding the peer services, sets options and adds interceptors.
        /// </summary>
        internal async Task StartListeningAsync()
        {
            ServerServiceDefinition serviceDefinition = PeerService.BindService(_serverService);

            // authentication interceptor
            if (_authInterceptor != null)
                serviceDefinition = serviceDefinition.Intercept(_authInterceptor);

            // setup the server
            _server = new Server(new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength)
            })
            {
                Services = {serviceDefinition},
                Ports =
                {
                    new ServerPort(IPAddress.Any.ToString(), NetworkOptions.ListeningPort, ServerCredentials.Insecure)
                }
            };

            // start listening
            await Task.Run(() => _server.Start());
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

            var taskList = NetworkOptions.BootNodes.Select(DialPeerAsync).ToList();
            await Task.WhenAll(taskList.ToArray<Task>());
        }
        
        /// <summary>
        /// Connects to a node with the given ip address and adds it to the node's peer pool.
        /// </summary>
        /// <param name="ipAddress">the ip address of the distant node</param>
        /// <returns>True if the connection was successful, false otherwise</returns>
        public async Task<bool> DialPeerAsync(string ipAddress)
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
                await CleanPeerAsync(peer);
                return false;
            }

            HandshakeError handshakeError = ValidateHandshake(peerHandshake, peerPubkey);
            if (handshakeError != HandshakeError.HandshakeOk)
            {
                Logger.LogWarning($"Invalid handshake [{handshakeError}] from {ipAddress} - {peerPubkey}");
                await CleanPeerAsync(peer);
                return false;
            }
            
            Logger.LogTrace($"Connected to {peer} - LIB height {peer.LastKnownLibHeight}, " +
                            $"best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}].");
            
            // TODO move to event handler in OS ?
            // await _nodeManager.AddNodeAsync(new Node { Pubkey = peerPubkey.ToByteString(), Endpoint = ipAddress});
            
            FireConnectionEvent(peer);

            return true;
        }

        private async Task CleanPeerAsync(GrpcPeer peer)
        {
            await peer.DisconnectAsync(false);
            await _peerPool.RemovePeerAsync(peer.Info.Pubkey, false); // remove and cleanup
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

            return HandshakeError.HandshakeOk;
        }
        
        private void FireConnectionEvent(GrpcPeer peer)
        {
            var blockAnnouncement = new BlockAnnouncement {
                BlockHash = peer.CurrentBlockHash,
                BlockHeight = peer.CurrentBlockHeight
            };
            
            var announcement = new AnnouncementReceivedEventData(blockAnnouncement, peer.Info.Pubkey);
            
            _ = EventBus.PublishAsync(announcement);
        }

        public async Task StopAsync(bool gracefulDisconnect = true)
        {
            try
            {
                await _server.KillAsync();
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