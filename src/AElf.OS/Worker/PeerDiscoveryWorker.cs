using System.Linq;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
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
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly IPeerPool _peerPool;
        
        public ILogger<PeerDiscoveryWorker> Logger { get; set; }

        public PeerDiscoveryWorker(AbpTimer timer, IPeerDiscoveryService peerDiscoveryService, IPeerPool peerPool) : base(timer)
        {
            _peerDiscoveryService = peerDiscoveryService;
            Timer.Period = NetworkConstants.DefaultDiscoveryPeriodInMilliSeconds;

            _peerPool = peerPool;
            
            Logger = NullLogger<PeerDiscoveryWorker>.Instance;
        }

        protected override async void DoWork()
        {
            var newNodes = await _peerDiscoveryService.UpdatePeersAsync();

            if (newNodes == null || newNodes.Nodes.Count <= 0)
            {
                Logger.LogDebug("Discovery: no new peers discovered");
                return;
            }

            Logger.LogDebug($"Discovery: new nodes discovered : {newNodes}.");

            foreach (var node in newNodes.Nodes)
            {
                int currentPeerCount = _peerPool.CurrentPeerCount();
                if (currentPeerCount >= NetworkOptions.MaxPeers)
                {
                    Logger.LogDebug($"Discovery: Max peers reached {currentPeerCount}, aborting add.");
                    break;
                }

                await _peerPool.AddPeerAsync(node.Endpoint);
            }
        }
    }
}