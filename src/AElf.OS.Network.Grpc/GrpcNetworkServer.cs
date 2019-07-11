using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkServer : IAElfNetworkServer, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly PeerService.PeerServiceBase _serverService;
        private readonly AuthInterceptor _authInterceptor;
        private readonly IPeerDialer _peerDialer;

        private Server _server;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }

        public GrpcNetworkServer(PeerService.PeerServiceBase serverService, IPeerPool peerPool, 
            AuthInterceptor authInterceptor, IPeerDialer peerDialer)
        {
            _serverService = serverService;
            _authInterceptor = authInterceptor;
            _peerDialer = peerDialer;
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
        /// Connects to a node with the given ip address and adds it to the peer pool.
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
                peer = await _peerDialer.DialPeerAsync(ipAddress);
            }
            catch (PeerDialException ex)
            {
                Logger.LogTrace($"Dial exception {ipAddress}: {ex.Message}.");
                return false;
            }
            
            var peerPubkey = peer.Info.Pubkey;

            if (!_peerPool.TryAddPeer(peer))
            {
                Logger.LogWarning($"Peer {peerPubkey} is already in the pool."); // todo: exception ?
                await peer.DisconnectAsync(false);
                return false;
            }

            var finalizeReply = await peer.DoHandshakeAsync();
            
            if (finalizeReply == null || !finalizeReply.Success)
            {
                Logger.LogWarning($"Could not finalize connection to {ipAddress} - {peerPubkey}");
                await _peerPool.RemovePeerAsync(peerPubkey, true); // remove and cleanup
                await peer.DisconnectAsync(false);
                return false;
            }
            
            Logger.LogTrace($"Connected to {peer} -- height {peer.Info.StartHeight}.");
            
            // TODO move to event handler in OS ?
            // await _nodeManager.AddNodeAsync(new Node { Pubkey = peerPubkey.ToByteString(), Endpoint = ipAddress});
            
            // TODO fire again
            FireConnectionEvent(connectReply, peerPubkey);

            return true;
        }
        
        private void FireConnectionEvent(ConnectReply connectReply, string pubKey)
        {
            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(new BlockAnnouncement
            {
                BlockHash = connectReply.Handshake.BestChainBlockHeader.GetHash(),
                BlockHeight = connectReply.Handshake.BestChainBlockHeader.Height
            }, pubKey));
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

            await _peerPool.ClearAllPeersAsync(gracefulDisconnect);
        }

        public void Dispose()
        {
            // TODO: implement dispose pattern
        }
    }
}