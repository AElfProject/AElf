using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
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
        
        private readonly NetworkOptions _networkOptions;

        private readonly PeerService.PeerServiceBase _serverService;
        private readonly AuthInterceptor _authInterceptor;

        private Server _server;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }

        public GrpcNetworkServer(IOptionsSnapshot<NetworkOptions> options, PeerService.PeerServiceBase serverService,
            IPeerPool peerPool, AuthInterceptor authInterceptor)
        {
            _serverService = serverService;
            _authInterceptor = authInterceptor;
            _peerPool = peerPool;
            _networkOptions = options.Value;

            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task StartAsync()
        {
            ServerServiceDefinition serviceDefinition = PeerService.BindService(_serverService);

            if (_authInterceptor != null)
                serviceDefinition = serviceDefinition.Intercept(_authInterceptor);
            
            _server = new Server
            {
                Services = { serviceDefinition },
                Ports =
                {
                    new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure)
                }
            };

            await Task.Run(() => _server.Start());

            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                List<Task<bool>> taskList = _networkOptions.BootNodes.Select(_peerPool.AddPeerAsync).ToList();
                await Task.WhenAll(taskList.ToArray<Task>());
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }
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

            foreach (var peer in _peerPool.GetPeers(true))
            {
                if (gracefulDisconnect)
                {
                    try
                    {
                        await peer.SendDisconnectAsync();
                    }
                    catch (RpcException e)
                    {
                        Logger.LogError(e, $"Error sending disconnect {peer}.");
                    }
                }

                await peer.StopAsync();
            }
        }

        public void Dispose()
        {
        }
    }
}