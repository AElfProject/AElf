using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class LocalParallelTransactionExecutingService : ITransactionExecutingService, ISingletonDependency
    {
        private readonly ITransactionGrouper _grouper;
        private readonly ITransactionExecutingService _plainExecutingService;
        public ILogger<LocalParallelTransactionExecutingService> Logger { get; set; }

        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService, IEnumerable<IExecutionPlugin> plugins)
        {
            _grouper = grouper;
            _plainExecutingService =
                new TransactionExecutingService(transactionResultService, smartContractExecutiveService, plugins);
            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken,
            bool throwException = false, BlockStateSet partialBlockStateSet = null)
        {
            Logger.LogTrace($"Entered parallel ExecuteAsync.");
            var transactions = transactionExecutingDto.Transactions.ToList();
            var blockHeader = transactionExecutingDto.BlockHeader;
            // TODO: Is it reasonable to allow throwing exception here
//            if (throwException)
//            {
//                throw new NotSupportedException(
//                    $"Throwing exception is not supported in {nameof(LocalParallelTransactionExecutingService)}.");
//            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (parallelizable, nonParallizable) = await _grouper.GroupAsync(transactions);
            stopwatch.Stop();
            Logger.LogTrace($"##: group {transactions.Count} txs with: {stopwatch.ElapsedMilliseconds} ms");
            
            stopwatch.Reset();
            stopwatch.Start();
            var tasks = parallelizable.AsParallel().Select(txns => ExecuteAndPreprocessResult(blockHeader, txns, cancellationToken,
                throwException, partialBlockStateSet));
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            Logger.LogTrace($"##: total execute time: {stopwatch.ElapsedMilliseconds} ms");
            
            Logger.LogTrace($"Executed parallelizables.");
            
            var returnSets = MergeResults(results, out var conflictingSets).Item1;
            var returnSetCollection = new ReturnSetCollection(returnSets);

            var updatedPartialBlockStateSet = returnSetCollection.ToBlockStateSet();
            updatedPartialBlockStateSet.MergeFrom(partialBlockStateSet?.Clone() ?? new BlockStateSet());
            
            Logger.LogTrace($"Merged results from parallelizables.");
            
            var nonParallelizableReturnSets = await _plainExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = nonParallizable
                },
                cancellationToken, throwException, updatedPartialBlockStateSet);
            
            Logger.LogTrace($"Merged results from non-parallelizables.");
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var executionReturnSets = await _plainExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = transactions
                },
                cancellationToken, throwException,
                partialBlockStateSet);
            var keys = new HashSet<string>(
                executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateAccesses.Keys)));
            stopwatch.Stop();
            Logger.LogTrace($"### group execute {transactions.Count} txs in {stopwatch.ElapsedMilliseconds} ms");
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