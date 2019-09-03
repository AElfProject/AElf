using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
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
        
        public ILogger<CrossChainValidationProvider> Logger { get; set; }
        
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

            var isParentChainBlockDataIndexed =
                _indexedCrossChainBlockDataDiscoveryService.TryDiscoverIndexedParentChainBlockDataAsync(block);
            Logger.LogTrace($"Try discovery indexed parent chain block data: {isParentChainBlockDataIndexed}");

            var isSideChainBlockDataIndexed =
                _indexedCrossChainBlockDataDiscoveryService.TryDiscoverIndexedSideChainBlockDataAsync(block);
            Logger.LogTrace($"Try discovery indexed side chain block data: {isSideChainBlockDataIndexed}");
            
            var extraData = ExtractCrossChainExtraData(block.Header);

            try
            {
                if (isSideChainBlockDataIndexed ^ (extraData != null))
                {
                    // cross chain extra data in block header should be null if no side chain block data indexed in contract 
                    return false;
                }

                if (!isParentChainBlockDataIndexed && !isSideChainBlockDataIndexed)
                    return true;

                var indexedCrossChainBlockData =
                    await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Header.Height);
                
                var res = true;
                
                if (isSideChainBlockDataIndexed)
                    res = ValidateBlockExtraDataAsync(indexedCrossChainBlockData, extraData);
                
                if (res)
                {
                    res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, block.Header.PreviousBlockHash,
                        block.Header.Height - 1);
                }
                
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

        private bool ValidateBlockExtraDataAsync(CrossChainBlockData crossChainBlockData, CrossChainExtraData extraData)
        {
            var txRootHashList = crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMerkleTreeRoot).ToList();
            var calculatedSideChainTransactionsRoot = BinaryMerkleTree.FromLeafNodes(txRootHashList).Root;

            return calculatedSideChainTransactionsRoot.Equals(extraData.SideChainTransactionsRoot);
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