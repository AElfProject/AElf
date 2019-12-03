using System;
using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Worker
{
    public class PeerDiscoveryWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly INetworkService _networkService;

        public new ILogger<PeerDiscoveryWorker> Logger { get; set; }

        public PeerDiscoveryWorker(AbpTimer timer, IPeerDiscoveryService peerDiscoveryService,
            INetworkService networkService) : base(timer)
        {
            _peerDiscoveryService = peerDiscoveryService;
            Timer.Period = NetworkConstants.DefaultDiscoveryPeriod;

            _networkService = networkService;

            Logger = NullLogger<PeerDiscoveryWorker>.Instance;
        }

        protected override async void DoWork()
        {
            await ProcessPeerDiscoveryJob();
        }

        internal async Task ProcessPeerDiscoveryJob()
        {
            var newNodes = await _peerDiscoveryService.DiscoverNodesAsync();

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
                    if (_networkService.IsPeerPoolFull())
                    {
                        Logger.LogDebug("Peer pool is full, aborting add.");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception in discovery worker.");
                    continue;
                }

                await _networkService.AddPeerAsync(node.Endpoint);
            }
        }
    }
}