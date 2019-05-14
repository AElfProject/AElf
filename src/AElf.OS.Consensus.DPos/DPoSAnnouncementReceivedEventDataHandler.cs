using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Consensus.DPos
{
    public class DPoSAnnouncementReceivedEventDataHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IAEDPoSLastLastIrreversibleBlockDiscoveryService _idpoSLastLastIrreversibleBlockDiscoveryService;
        private readonly IBlockchainService _blockchainService;

        public DPoSAnnouncementReceivedEventDataHandler(ITaskQueueManager taskQueueManager,
            IAEDPoSLastLastIrreversibleBlockDiscoveryService idpoSLastLastIrreversibleBlockDiscoveryService,
            IBlockchainService blockchainService)
        {
            _taskQueueManager = taskQueueManager;
            _idpoSLastLastIrreversibleBlockDiscoveryService = idpoSLastLastIrreversibleBlockDiscoveryService;
            _blockchainService = blockchainService;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var irreversibleBlockIndex =
                await _idpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync(
                    eventData.SenderPubKey);

            if (irreversibleBlockIndex != null)
            {
                _taskQueueManager.Enqueue(async () =>
                {
                    var chain = await _blockchainService.GetChainAsync();
                    if (chain.LastIrreversibleBlockHeight < irreversibleBlockIndex.Height)
                    {
                        var hash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                            irreversibleBlockIndex.Height, chain.BestChainHash);
                        if (hash == irreversibleBlockIndex.Hash)
                        {
                            await _blockchainService.SetIrreversibleBlockAsync(chain, irreversibleBlockIndex.Height,
                                irreversibleBlockIndex.Hash);
                        }
                    }
                }, KernelConstants.UpdateChainQueueName);
            }
        }
    }

    public interface IAEDPoSLastLastIrreversibleBlockDiscoveryService
    {
        Task<IBlockIndex> FindLastLastIrreversibleBlockAsync(string senderPubKey);
    }

    public class AEDPoSLastLastIrreversibleBlockDiscoveryService : IAEDPoSLastLastIrreversibleBlockDiscoveryService,
        ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly IAEDPoSInformationProvider _dpoSInformationProvider;
        private readonly IBlockchainService _blockchainService;
        public ILogger<AEDPoSLastLastIrreversibleBlockDiscoveryService> Logger { get; set; }

        public AEDPoSLastLastIrreversibleBlockDiscoveryService(IPeerPool peerPool,
            IAEDPoSInformationProvider dpoSInformationProvider, IBlockchainService blockchainService)
        {
            _peerPool = peerPool;
            _dpoSInformationProvider = dpoSInformationProvider;
            _blockchainService = blockchainService;
            //LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<IBlockIndex> FindLastLastIrreversibleBlockAsync(string senderPubKey)
        {
            var senderPeer = _peerPool.FindPeerByPublicKey(senderPubKey);

            if (senderPeer == null)
                return null;

            var orderedBlocks = senderPeer.RecentBlockHeightAndHashMappings.OrderByDescending(p => p.Key).ToList();

            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var pubkeyList = (await _dpoSInformationProvider.GetCurrentMinerList(chainContext)).ToList();

            var peers = _peerPool.GetPeers().Where(p => pubkeyList.Contains(p.PubKey)).ToList();

            if (peers.Count == 0)
                return null;

            foreach (var block in orderedBlocks)
            {
                var peersHadBlockAmount = peers.Where(p =>
                {
                    p.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var hash);
                    return hash == block.Value;
                }).Count();

                var sureAmount = (int) (pubkeyList.Count * 2d / 3);
                if (peersHadBlockAmount >= sureAmount)
                {
                    Logger.LogDebug($"LIB found in network layer: height {block.Key}");
                    return new BlockIndex(block.Value, block.Key);
                }
            }

            return null;
        }
    }
}