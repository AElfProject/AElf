using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IIndexedCrossChainBlockDataDiscoveryService _indexedCrossChainBlockDataDiscoveryService;
        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }
        
        public CrossChainValidationProvider(ICrossChainIndexingDataService crossChainIndexingDataService, 
            IBlockExtraDataService blockExtraDataService, IIndexedCrossChainBlockDataDiscoveryService indexedCrossChainBlockDataDiscoveryService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _blockExtraDataService = blockExtraDataService;
            _indexedCrossChainBlockDataDiscoveryService = indexedCrossChainBlockDataDiscoveryService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Header.Height == Constants.GenesisBlockHeight)
                return true;

            var isCrossChainDataIndexed = _indexedCrossChainBlockDataDiscoveryService.TryDiscoverCrossChainBlockDataAsync(block);
            var extraData = ExtractCrossChainExtraData(block.Header);

            try
            {
                if (isCrossChainDataIndexed ^ (extraData != null))
                {
                    // cross chain extra data in block header should be null if nothing indexed in contract 
                    return false;
                }

                if (!isCrossChainDataIndexed)
                    return true;

                var indexedCrossChainBlockData =
                    await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Header.Height);
                var res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, extraData, block);
                return res;
            }
            catch (ValidateNextTimeBlockValidationException ex)
            {
                throw new BlockValidationException(
                    $"Cross chain data is not ready at height: {block.Header.Height}, hash: {block.GetHash()}.", ex);
            }
            finally
            {
                await LocalEventBus.PublishAsync(new CrossChainDataValidatedEvent());
            }
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, 
            CrossChainExtraData extraData, IBlock block)
        {
            var txRootHashList = crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMerkleTreeRoot).ToList();
            var calculatedSideChainTransactionsRoot = txRootHashList.ComputeBinaryMerkleTreeRootWithLeafNodes();
            
            // first check identity with the root in header
            if (extraData != null && !calculatedSideChainTransactionsRoot.Equals(extraData.SideChainTransactionsRoot) ||
                extraData == null && !calculatedSideChainTransactionsRoot.Equals(Hash.Empty))
                return false;
            
            // check cache identity
            var res = await ValidateCrossChainBlockDataAsync(crossChainBlockData, block.Header.PreviousBlockHash,
                block.Header.Height - 1);
            return res;
        }

        private CrossChainExtraData ExtractCrossChainExtraData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader("CrossChain", header);
            return bytes == ByteString.Empty || bytes == null ? null : CrossChainExtraData.Parser.ParseFrom(bytes);
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, 
            Hash blockHash, long blockHeight)
        {
            if (CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored)
                return true;
            
            var sideChainBlockDataValidationResult =
                await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(
                    crossChainBlockData.SideChainBlockData.ToList(), blockHash, blockHeight);
            if (!sideChainBlockDataValidationResult)
                return false;
            
            var parentChainBlockDataValidationResult =
                await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(
                    crossChainBlockData.ParentChainBlockData.ToList(), blockHash, blockHeight);
            if (!parentChainBlockDataValidationResult)
                return false;

            return true;
        }
    }
}