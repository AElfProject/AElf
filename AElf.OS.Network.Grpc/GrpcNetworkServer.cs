using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        private readonly PeerService.PeerServiceBase _serverService;
        
        private readonly IPeerPool _peerPool;

        private Server _server;
        
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        public GrpcNetworkServer(IOptionsSnapshot<NetworkOptions> options, PeerService.PeerServiceBase serverService, 
            IPeerPool peerPool)
        {
            _serverService = serverService;
            _peerPool = peerPool;
            _networkOptions = options.Value;
            
            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }
        
        public async Task StartAsync()
        {
            _server = new Server {
                Services = { PeerService.BindService(_serverService) },
                Ports = { new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure) }
            };
            
            await Task.Run(() => _server.Start());
            
            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                Stopwatch s = Stopwatch.StartNew();
                List<Task<bool>> taskList = _networkOptions.BootNodes.Select(_peerPool.AddPeerAsync).ToList();
                await Task.WhenAll(taskList.ToArray<Task>());
                s.Stop();
                Logger.LogDebug($"ELAPSED: {s.ElapsedMilliseconds} ms");
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }
        }
        
        public async Task StopAsync()
        {
            await _server.KillAsync();
            
            foreach (var peer in _peerPool.GetPeers())
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