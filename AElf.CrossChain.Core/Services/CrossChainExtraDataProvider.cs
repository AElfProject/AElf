using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public class CrossChainExtraDataProvider : IBlockValidationProvider, IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;

        public CrossChainExtraDataProvider(ITransactionResultQueryService transactionResultQueryService, 
            ICrossChainService crossChainService, IBlockExtraDataService blockExtraDataService)
        {
            _transactionResultQueryService = transactionResultQueryService;
            _crossChainService = crossChainService;
            _blockExtraDataService = blockExtraDataService;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            try
            {
                if (!CrossChainEventHelper.TryGetLogEventInBlock(block, out var logEvent) ||
                    await ValidateCrossChainLogEventInBlock(logEvent, block))
                    return true; // no event means no indexing.
                throw new Exception();
            }
            catch (Exception e)
            {
                throw new ValidateNextTimeBlockValidationException("Cross chain validation failed after execution.", e);
            }
        }

        private async Task<bool> ValidateCrossChainLogEventInBlock(LogEvent interestedLogEvent, IBlock block)
        {
            foreach (var txId in block.Body.Transactions)
            {
                var res = await _transactionResultQueryService.GetTransactionResultAsync(txId);
                var sideChainTransactionsRoot =
                    CrossChainEventHelper.TryGetValidateCrossChainBlockData(res, block, interestedLogEvent,
                        out var crossChainBlockData);
                // first check equality with the root in header
                if (sideChainTransactionsRoot == null
                    || !sideChainTransactionsRoot.Equals(Hash.LoadByteArray(
                        _blockExtraDataService.GetBlockExtraData(GetType(), block.Header).ToByteArray())))
                    continue;
                return await ValidateCrossChainBlockDataAsync(crossChainBlockData,
                    block.Header.GetHash(), block.Header.Height);
            }
            return false;
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData,
            Hash preBlockHash, long preBlockHeight)
        {
            return await _crossChainService.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData, preBlockHash, preBlockHeight) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData, preBlockHash, preBlockHeight);
        }

        public Task<ByteString> GetExtraDataAsync(BlockHeader blockHeader)
        {
            throw new NotImplementedException();
        }
    }
}