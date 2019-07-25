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
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly PeerService.PeerServiceBase _serverService;
        private readonly IConnectionService _connectionService;
        private readonly AuthInterceptor _authInterceptor;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        private Server _server;

        public GrpcNetworkServer(PeerService.PeerServiceBase serverService, IConnectionService connectionService, AuthInterceptor authInterceptor)
        {
            _serverService = serverService;
            _connectionService = connectionService;
            _authInterceptor = authInterceptor;

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
        internal Task StartListeningAsync()
        {
            ServerServiceDefinition serviceDefinition = PeerService.BindService(_serverService);

            if (_authInterceptor != null)
                serviceDefinition = serviceDefinition.Intercept(_authInterceptor);

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
            
            return Task.Run(() => _server.Start());
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

            var taskList = NetworkOptions.BootNodes
                .Select(async s => await _connectionService.ConnectAsync(s)).ToList();
            
            await Task.WhenAll(taskList.ToArray<Task>());
        }

        /// <summary>
        /// Connects to a node with the given ip address and adds it to the node's peer pool.
        /// </summary>
        /// <param name="ipAddress">the ip address of the distant node</param>
        /// <returns>True if the connection was successful, false otherwise</returns>
        public async Task<bool> ConnectAsync(string ipAddress)
        {
            return await _connectionService.ConnectAsync(ipAddress);
        }

        public async Task DisconnectAsync(IPeer peer, bool sendDisconnect = false)
        {
            await _connectionService.DisconnectAsync(peer, sendDisconnect);
        }

        public async Task StopAsync(bool gracefulDisconnect = true)
        {
            try
            {
                await _server.ShutdownAsync();
            }
            catch (InvalidOperationException)
            {
                // if server already shutdown, we continue and clear the channels.
            }

            await _connectionService.DisconnectPeersAsync(gracefulDisconnect);
        }
    }
}