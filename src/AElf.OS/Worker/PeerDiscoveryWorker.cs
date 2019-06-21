using System.Linq;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Worker
{
    public class PeerDiscoveryWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        
        public ILogger<PeerDiscoveryWorker> Logger { get; set; }

        public PeerDiscoveryWorker(AbpTimer timer, IPeerDiscoveryService peerDiscoveryService) : base(timer)
        {
            _peerDiscoveryService = peerDiscoveryService;
            Timer.Period = NetworkConstants.DefaultNodeDiscoveryInMilliSeconds;
            
            Logger = NullLogger<PeerDiscoveryWorker>.Instance;
        }

        protected override async void DoWork()
        {
            var newNodes = await _peerDiscoveryService.UpdatePeersAsync();

            var s = string.Join(", ", newNodes.Nodes.Select(n => $"{{ pubKey: {n.Pubkey}, port: {n.Endpoint} }}").ToList());
            Logger.LogDebug($"Node discovered : {s}.");
        }
    }
}