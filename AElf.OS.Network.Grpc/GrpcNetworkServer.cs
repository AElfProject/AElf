using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkServer : IAElfNetworkServer, ISingletonDependency
    {
        private readonly NetworkOptions _networkOptions;
        
        private readonly IAElfServerService _serverService;
        private readonly IPeerManager _peerManager;

        private Server _server;
        
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        public GrpcNetworkServer(IOptionsSnapshot<NetworkOptions> options, IAElfServerService serverService, 
            IPeerManager peerManager)
        {
            _serverService = serverService;
            _peerManager = peerManager;
            _networkOptions = options.Value;
            
            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }
        
        public async Task StartAsync()
        {
            _server = new Server {
                Services = { PeerService.BindService(_serverService as PeerService.PeerServiceBase) },
                Ports = { new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure) }
            };
            
            await Task.Run(() => _server.Start());
            
            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                List<Task<bool>> taskList = _networkOptions.BootNodes.Select(_peerManager.AddPeerAsync).ToList();
                await Task.WhenAll(taskList.ToArray<Task>());
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }
        }
        
        public async Task StopAsync()
        {
            await _server.KillAsync();
            
            foreach (var peer in _peerManager.GetPeers())
            {
                try
                {
                    await peer.SendDisconnectAsync();
                    await peer.StopAsync();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Error while disconnecting peer {peer}.");
                }
            }
        }
    }
}