using System;
using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.OS.Worker
{
    public class PeerDiscoveryWorker : AsyncPeriodicBackgroundWorkerBase
    {
        public new ILogger<PeerDiscoveryWorker> Logger { get; set; }

        public PeerDiscoveryWorker(AbpTimer timer, 
            IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
        {
            Timer.Period = NetworkConstants.DefaultDiscoveryPeriod;
            
            Logger = NullLogger<PeerDiscoveryWorker>.Instance;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await ProcessPeerDiscoveryJob(workerContext);
        }

        internal async Task ProcessPeerDiscoveryJob(PeriodicBackgroundWorkerContext workerContext)
        {
            var networkService = workerContext.ServiceProvider.GetRequiredService<INetworkService>();
            var reconnectionService = workerContext.ServiceProvider.GetRequiredService<IReconnectionService>();
            var peerDiscoveryService = workerContext.ServiceProvider.GetRequiredService<IPeerDiscoveryService>();
            
            var newNodes = await peerDiscoveryService.DiscoverNodesAsync();

            if (newNodes == null || newNodes.Nodes.Count <= 0)
            {
                Logger.LogDebug("No new nodes discovered");
                return;
            }

            Logger.LogDebug($"New nodes discovered : {newNodes}.");

            foreach (var node in newNodes.Nodes)
            {
                try
                {
                    var reconnectingPeer = reconnectionService.GetReconnectingPeer(node.Endpoint);

                    if (reconnectingPeer != null)
                    {
                        Logger.LogDebug($"Peer {node.Endpoint} is already in the reconnection queue.");
                        continue;
                    }
                    
                    if (networkService.IsPeerPoolFull())
                    {
                        Logger.LogDebug("Peer pool is full, aborting add.");
                        break;
                    }
                    
                    await networkService.AddPeerAsync(node.Endpoint);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Exception connecting to {node.Endpoint}.");
                }
            }
        }
    }
}