using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel
{
    public class LocalParallelTransactionExecutingService : ILocalParallelTransactionExecutingService
    {
        private readonly ITransactionGrouper _grouper;
        private readonly ITransactionExecutingService _plainExecutingService;

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            ITransactionExecutingService plainExecutingService)
        {
            _grouper = grouper;
            _plainExecutingService = plainExecutingService;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(BlockHeader blockHeader,
            List<Transaction> transactions, CancellationToken cancellationToken, bool throwException = false,
            BlockStateSet partialBlockStateSet = null)
        {
            if (throwException)
            {
                throw new NotSupportedException(
                    $"Throwing exception is not supported in {nameof(LocalParallelTransactionExecutingService)}.");
            }

            var groups = new List<List<Transaction>>();
            var tasks = groups.Select(txns => ExecuteAndPreprocessResult(blockHeader, txns, cancellationToken,
                throwException, partialBlockStateSet));
            var results = await Task.WhenAll(tasks);

            return MergeResults(results).Item1;
        }

        private async Task<(List<ExecutionReturnSet>, HashSet<string>)> ExecuteAndPreprocessResult(
            BlockHeader blockHeader, List<Transaction> transactions, CancellationToken cancellationToken,
            bool throwException = false, BlockStateSet partialBlockStateSet = null)
        {
            var executionReturnSets = await _plainExecutingService.ExecuteAsync(blockHeader, transactions,
                cancellationToken, throwException,
                partialBlockStateSet);
            var keys = new HashSet<string>(executionReturnSets.SelectMany(s => s.StateChanges.Keys));
            return (executionReturnSets, keys);
        }

        private (List<ExecutionReturnSet>, HashSet<string>) MergeResults(
            IEnumerable<(List<ExecutionReturnSet>, HashSet<string>)> results)
        {
            var returnSets = new List<ExecutionReturnSet>();
            var existingKeys = new HashSet<string>();
            foreach (var (sets, keys) in results)
            {
                if (!existingKeys.Overlaps(keys))
                {
                    returnSets.AddRange(sets);
                }

                // TODO: Fire event if overlaps
            }

            return (returnSets, existingKeys);
        }
    }
}