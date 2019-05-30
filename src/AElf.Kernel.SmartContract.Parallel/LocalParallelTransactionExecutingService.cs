using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class LocalParallelTransactionExecutingService : ILocalParallelTransactionExecutingService
    {
        private readonly ITransactionGrouper _grouper;
        private readonly ITransactionExecutingService _plainExecutingService;

        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService, IEnumerable<IExecutionPlugin> plugins)
        {
            _grouper = grouper;
            _plainExecutingService =
                new TransactionExecutingService(transactionResultService, smartContractExecutiveService, plugins);
            EventBus = NullLocalEventBus.Instance;
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

            var (parallelizable, nonParallizable) = await _grouper.GroupAsync(transactions);
            var tasks = parallelizable.Select(txns => ExecuteAndPreprocessResult(blockHeader, txns, cancellationToken,
                throwException, partialBlockStateSet));
            var results = await Task.WhenAll(tasks);

            var returnSets = MergeResults(results, out var conflictingSets).Item1;
            var returnSetCollection = new ReturnSetCollection(returnSets);

            var updatedPartialBlockStateSet = returnSetCollection.ToBlockStateSet();
            updatedPartialBlockStateSet.MergeFrom(partialBlockStateSet?.Clone() ?? new BlockStateSet());

            var nonParallelizableReturnSets = await _plainExecutingService.ExecuteAsync(blockHeader, nonParallizable,
                cancellationToken, throwException, updatedPartialBlockStateSet);
            returnSets.AddRange(nonParallelizableReturnSets);
            if (conflictingSets.Count > 0)
            {
                // TODO: Add event handler somewhere
                await EventBus.PublishAsync(
                    new ConflictingTransactionsFoundInParallelGroupsEvent(returnSets, conflictingSets));
            }

            return returnSets;
        }

        private async Task<(List<ExecutionReturnSet>, HashSet<string>)> ExecuteAndPreprocessResult(
            BlockHeader blockHeader, List<Transaction> transactions, CancellationToken cancellationToken,
            bool throwException = false, BlockStateSet partialBlockStateSet = null)
        {
            var executionReturnSets = await _plainExecutingService.ExecuteAsync(blockHeader, transactions,
                cancellationToken, throwException,
                partialBlockStateSet);
            var keys = new HashSet<string>(
                executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateAccesses.Keys)));
            return (executionReturnSets, keys);
        }

        private (List<ExecutionReturnSet>, HashSet<string>) MergeResults(
            IEnumerable<(List<ExecutionReturnSet>, HashSet<string>)> results,
            out List<ExecutionReturnSet> conflictingSets)
        {
            var returnSets = new List<ExecutionReturnSet>();
            conflictingSets = new List<ExecutionReturnSet>();
            var existingKeys = new HashSet<string>();
            foreach (var (sets, keys) in results)
            {
                if (!existingKeys.Overlaps(keys))
                {
                    returnSets.AddRange(sets);
                }
                else
                {
                    conflictingSets.AddRange(sets);
                }
            }

            return (returnSets, existingKeys);
        }
    }
}