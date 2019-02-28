using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly ITransactionResultManager _transactionResultManager;

        public CrossChainValidationProvider(ITransactionResultManager transactionResultManager, 
            ICrossChainService crossChainService)
        {
            _transactionResultManager = transactionResultManager;
            _crossChainService = crossChainService;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block)
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
                var res = await _transactionResultManager.GetTransactionResultAsync(txId);
                var sideChainTransactionsRoot =
                    CrossChainEventHelper.TryGetValidateCrossChainBlockData(res, block, interestedLogEvent,
                        out var crossChainBlockData);
                // first check equality with the root in header
                if(sideChainTransactionsRoot == null 
                   || !sideChainTransactionsRoot.Equals(block.Header.BlockExtraData.SideChainTransactionsRoot))
                    continue;
                return await ValidateCrossChainBlockDataAsync(block.Header.ChainId, crossChainBlockData,
                    block.Header.GetHash(), block.Header.Height);
            }
            return false;
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(int chainId, CrossChainBlockData crossChainBlockData,
            Hash preBlockHash, ulong preBlockHeight)
        {
            return await _crossChainService.ValidateSideChainBlockDataAsync(chainId,
                       crossChainBlockData.SideChainBlockData, preBlockHash, preBlockHeight) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(chainId,
                       crossChainBlockData.ParentChainBlockData, preBlockHash, preBlockHeight);
        }
    }
}