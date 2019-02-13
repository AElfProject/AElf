using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;

namespace AElf.Execution.Execution
{
    public class BlockExecutingService : IBlockExecutingService
    {
        private readonly IExecutingService _executingService;
        private readonly IBlockManager _blockManager;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public BlockExecutingService(IExecutingService executingService, IBlockManager blockManager, IBlockchainStateManager blockchainStateManager)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockchainStateManager = blockchainStateManager;
        }

        public async Task ExecuteBlockAsync(int chainId, Hash blockHash)
        {
            var block = await _blockManager.GetBlockAsync(blockHash);
            var readyTxs = block.Body.TransactionList.ToList();
            // TODO: Use BlockStateSet to calculate merkle tree
            var traces = await ExecuteTransactions(readyTxs, block.Header.ChainId,
                block.Header.Time.ToDateTime(), block.Header.GetDisambiguationHash(), CancellationToken.None);
            var blockStateSet = new BlockStateSet()
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height,
                PreviousHash = block.Header.PreviousBlockHash
            };
            FillBlockStateSet(blockStateSet, traces);
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
        }

        private async Task<List<TransactionTrace>> ExecuteTransactions(List<Transaction> readyTxs, int chainId,
            DateTime toDateTime, Hash disambiguationHash, CancellationToken cancellationToken)
        {
            var traces = readyTxs.Count == 0
                ? new List<TransactionTrace>()
                : await _executingService.ExecuteAsync(readyTxs, chainId, toDateTime, cancellationToken,
                    disambiguationHash);
            return traces;
        }

        private void FillBlockStateSet(BlockStateSet blockStateSet, IEnumerable<TransactionTrace> traces)
        {
            foreach (var trace in traces)
            {
                foreach (var w in trace.GetFlattenedWrite())
                {
                    blockStateSet.Changes[w.Key] = w.Value;
                }
            }
        }
    }
}