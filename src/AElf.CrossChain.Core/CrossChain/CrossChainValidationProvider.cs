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
                if (!isSideChainBlockDataIndexed && extraData != null)
                {
                    // cross chain extra data in block header should be null if no side chain block data indexed in contract 
                    Logger.LogWarning(
                        $"Cross chain extra data should not be null, block height {block.Header.Height}, hash {block.GetHash()}.");
                    return false;
                }

                if (!isParentChainBlockDataIndexed && !isSideChainBlockDataIndexed)
                    return true;

                var indexedCrossChainBlockData =
                    await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Header.Height);
                
                if (isSideChainBlockDataIndexed && indexedCrossChainBlockData.SideChainBlockData.Count > 0)
                {
                    if (extraData == null || !ValidateBlockExtraDataAsync(indexedCrossChainBlockData, extraData))
                    {
                        // extra data is null, or it is not consistent with contract
                        Logger.LogWarning(
                            $"Invalid cross chain extra data, block height {block.Header.Height}, hash {block.GetHash()}.");
                        return false;
                    }
                }

                var validationResult = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, block.Header.PreviousBlockHash,
                    block.Header.Height - 1);
                
                return validationResult;
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
            var txRootHashList = crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionStatusMerkleTreeRoot).ToList();
            var calculatedSideChainTransactionsRoot = BinaryMerkleTree.FromLeafNodes(txRootHashList).Root;

            return calculatedSideChainTransactionsRoot.Equals(extraData.TransactionStatusMerkleTreeRoot);
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
            {
                Logger.LogTrace("Cross chain data validation disabled.");
                return true;
            }
            
            var sideChainBlockDataValidationResult =
                await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(
                    crossChainBlockData.SideChainBlockData.ToList(), blockHash, blockHeight);
            if (!sideChainBlockDataValidationResult)
                return false;
            
            var parentChainBlockDataValidationResult =
                await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(
                    crossChainBlockData.ParentChainBlockData.ToList(), blockHash, blockHeight);
            
            return parentChainBlockDataValidationResult;
        }
    }
}