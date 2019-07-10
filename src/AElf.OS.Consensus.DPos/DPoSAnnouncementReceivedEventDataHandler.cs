using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Consensus.DPos
{
    public class DPoSAnnouncementReceivedEventDataHandler : ILocalEventHandler<PreLibConfirmAnnouncementReceivedEventData>
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

        public async Task HandleEventAsync(PreLibConfirmAnnouncementReceivedEventData eventData)
        {
            var irreversibleBlockIndex =
                await _idpoSLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockAsync();

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
        Task<IBlockIndex> FindLastLastIrreversibleBlockAsync();
    }

    public class AEDPoSLastLastIrreversibleBlockDiscoveryService : IAEDPoSLastLastIrreversibleBlockDiscoveryService,
        ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly IAEDPoSInformationProvider _dpoSInformationProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;
        public ILogger<AEDPoSLastLastIrreversibleBlockDiscoveryService> Logger { get; set; }

        public AEDPoSLastLastIrreversibleBlockDiscoveryService(IPeerPool peerPool,
            IAEDPoSInformationProvider dpoSInformationProvider, IBlockchainService blockchainService,
            IAccountService accountService)
        {
            _peerPool = peerPool;
            _dpoSInformationProvider = dpoSInformationProvider;
            _blockchainService = blockchainService;
            _accountService = accountService;
            //LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<IBlockIndex> FindLastLastIrreversibleBlockAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            var preLibHeight = chain.BestChainHeight - 10;
            if (preLibHeight <= chain.LastIrreversibleBlockHeight) return null;
            
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var pubkeyList = (await _dpoSInformationProvider.GetCurrentMinerList(chainContext)).ToList();

            var peers = _peerPool.GetPeers().Where(p => pubkeyList.Contains(p.Info.Pubkey)).ToList();

            var pubKey = (await _accountService.GetPublicKeyAsync()).ToHex();
            if (peers.Count == 0 && !pubkeyList.Contains(pubKey)) return null;

            var sureAmount = pubkeyList.Count;
            var hasBlock = _peerPool.RecentBlockHeightAndHashMappings.TryGetValue(preLibHeight, out var blockInfo) && !blockInfo.HasFork;
            if (!hasBlock) return null;

            var blockHash = blockInfo.BlockHash;
            if (!_peerPool.PreLibBlockHeightAndHashMappings.TryGetValue(preLibHeight, out var preLibBlockInfo) ||
                preLibBlockInfo.BlockHash != blockHash || preLibBlockInfo.PreLibCount < sureAmount)
                return null;

            var peersHadBlockCount = peers.Count(p =>
                p.HasBlock(preLibHeight, blockHash) &&
                p.PreLibBlockHeightAndHashMappings.TryGetValue(preLibHeight, out var preLibBlock) &&
                preLibBlock.BlockHash == blockHash && preLibBlock.PreLibCount >= sureAmount);
            
            if (pubkeyList.Contains(pubKey))
                peersHadBlockCount++;
            if (peersHadBlockCount < sureAmount) return null;

            Logger.LogDebug($"LIB found in network layer: height {preLibHeight}");
            return new BlockIndex(blockHash, preLibHeight);
        }
    }
}