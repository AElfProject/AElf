using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IBlockExtraDataOrderService _blockExtraDataOrderService;

        public CrossChainValidationProvider(ICrossChainService crossChainService, IBlockExtraDataOrderService blockExtraDataOrderService)
        {
            _crossChainService = crossChainService;
            _blockExtraDataOrderService = blockExtraDataOrderService;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
//                if (!CrossChainEventHelper.TryGetLogEventInBlock(block, out var logEvent) ||
//                    await ValidateCrossChainLogEventInBlock(logEvent, block))
//                    return true; // no event means no indexing.
//                throw new Exception();
            var indexedCrossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            if (indexedCrossChainBlockData == null)
                return true;
            bool res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, block.Header.BlockExtraData.SideChainTransactionsRoot,
                block.GetHash(), block.Height);
            if(!res)
                throw new ValidateNextTimeBlockValidationException("Cross chain validation failed after execution.");
            return true;
        }

//        private async Task<bool> ValidateCrossChainLogEventInBlock(LogEvent interestedLogEvent, IBlock block)
//        {
//            foreach (var txId in block.Body.Transactions)
//            {
//                var res = await _transactionResultManager.GetTransactionResultAsync(txId);
//                var sideChainTransactionsRoot =
//                    CrossChainEventHelper.TryGetValidateCrossChainBlockData(res, block, interestedLogEvent,
//                        out var crossChainBlockData);
//                // first check equality with the root in header
//                if(sideChainTransactionsRoot == null 
//                   || !sideChainTransactionsRoot.Equals(block.Header.BlockExtraData.SideChainTransactionsRoot))
//                    continue;
//                return await ValidateCrossChainBlockDataAsync(crossChainBlockData,
//                    block.Header.GetHash(), block.Header.Height);
//            }
//            return false;
//        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, Hash sideChainTransactionsRoot,
            Hash preBlockHash, long preBlockHeight)
        {
            var txRootHashList = crossChainBlockData.ParentChainBlockData.Select(pcb => pcb.Root.SideChainTransactionsRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            // first check equality with the root in header
            if (sideChainTransactionsRoot != null && !calculatedSideChainTransactionsRoot.Equals(sideChainTransactionsRoot)
            || !sideChainTransactionsRoot.Equals(Hash.LoadByteArray(block.Header
                                       .BlockExtraDatas[
                                           _blockExtraDataOrderService.GetExtraDataProviderOrder(
                                               typeof(CrossChainBlockExtraDataProvider))].ToByteArray())))
                return false;
            
            return await _crossChainService.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData, preBlockHash, preBlockHeight) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData, preBlockHash, preBlockHeight);
        }
    }
}