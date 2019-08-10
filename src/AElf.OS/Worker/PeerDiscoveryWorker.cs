using System;
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
            Timer.Period = NetworkConstants.DefaultDiscoveryPeriodInMilliSeconds;

            _networkService = networkService;

            Logger = NullLogger<PeerDiscoveryWorker>.Instance;
        }

        protected override async void DoWork()
        {
            try
            {
                var newNodes = await _peerDiscoveryService.DiscoverNodesAsync();

                if (newNodes == null || newNodes.Nodes.Count <= 0)
                {
                    Logger.LogDebug("Discovery: no new nodes discovered");
                    return;
                }

                Logger.LogDebug($"Discovery: new nodes discovered : {newNodes}.");

                foreach (var node in newNodes.Nodes)
                {
                    if (_networkService.IsPeerPoolFull())
                    {
                        Logger.LogDebug("Discovery: Peer pool is full, aborting add.");
                        break;
                    }

                    await _networkService.AddPeerAsync(node.Endpoint);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception in discovery worker.");
            }
        }
    }
}