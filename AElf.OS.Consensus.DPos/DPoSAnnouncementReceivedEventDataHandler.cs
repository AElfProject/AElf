using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Consensus.DPos
{
    public class DPoSAnnouncementReceivedEventDataHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private ITaskQueueManager _taskQueueManager;
        private IDPoSLastLastIrreversibleBlockDiscoveryService _idpoSLastLastIrreversibleBlockDiscoveryService;

        public DPoSAnnouncementReceivedEventDataHandler(ITaskQueueManager taskQueueManager,
            IDPoSLastLastIrreversibleBlockDiscoveryService idpoSLastLastIrreversibleBlockDiscoveryService)
        {
            _taskQueueManager = taskQueueManager;
            _idpoSLastLastIrreversibleBlockDiscoveryService = idpoSLastLastIrreversibleBlockDiscoveryService;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var hash = _idpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockHashAsync(
                eventData.SenderPubKey);

            if (hash != null)
            {
                _taskQueueManager.Enqueue(() =>
                {
                    
                    //TODO: should call set LIB
                    return Task.CompletedTask;
                });
            }
            
        }
    }


    public interface IDPoSLastLastIrreversibleBlockDiscoveryService
    {
        Task<Hash> FindLastLastIrreversibleBlockHashAsync(string senderPubKey);
    }

    public class DPoSLastLastIrreversibleBlockDiscoveryService : IDPoSLastLastIrreversibleBlockDiscoveryService,
        ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly IDPoSInformationProvider _dpoSInformationProvider;
        private readonly IBlockchainService _blockchainService;

        public DPoSLastLastIrreversibleBlockDiscoveryService(IPeerPool peerPool,
            IDPoSInformationProvider dpoSInformationProvider, IBlockchainService blockchainService)
        {
            _peerPool = peerPool;
            _dpoSInformationProvider = dpoSInformationProvider;
            _blockchainService = blockchainService;
            //LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<Hash> FindLastLastIrreversibleBlockHashAsync(string senderPubKey)
        {
            var senderPeer = _peerPool.FindPeerByPublicKey(senderPubKey);

            if (senderPeer == null)
                return null;

            var orderedBlocks = senderPeer.RecentBlockHeightAndHashMappings.OrderBy(p => p.Key).ToList();

            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var currentMiners = await _dpoSInformationProvider.GetCurrentMiners(chainContext);
            
            var pubkeyList = currentMiners.PublicKeys;
            
            var peers = _peerPool.GetPeers().Where(p=>pubkeyList.Contains( p.PubKey )).ToList();

            if (peers.Count == 0)
                return null;

            foreach (var block in orderedBlocks)
            {
                var peersHadBlockAmount = peers.Where(p =>
                {
                    p.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var hash);
                    return hash == block.Value;
                }).Count();

                if (peersHadBlockAmount > (int) (pubkeyList.Count * 2d / 3) + 1)
                {
                    return block.Value;
                }
            }


            return null;
        }
    }
}