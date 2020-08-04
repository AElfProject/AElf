using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Utilities.Collections;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class LocalParallelTransactionExecutingService : IParallelTransactionExecutingService, ISingletonDependency
    {
        private readonly ITransactionGrouper _grouper;
        private readonly IPlainTransactionExecutingService _planTransactionExecutingService;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;
        private readonly EvilTriggerOptions _evilTriggerOptions;

        public ILogger<LocalParallelTransactionExecutingService> Logger { get; set; }
        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            IPlainTransactionExecutingService planTransactionExecutingService,
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOption)
        {
            _grouper = grouper;
            _planTransactionExecutingService = planTransactionExecutingService;
            _systemTransactionExtraDataProvider = systemTransactionExtraDataProvider;
            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;

            _evilTriggerOptions = evilTriggerOption.CurrentValue;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entered parallel ExecuteAsync.");
            var transactions = transactionExecutingDto.Transactions.ToList();
            var blockHeader = transactionExecutingDto.BlockHeader;

            var chainContext = new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash,
                BlockHeight = blockHeader.Height - 1
            };
            var groupedTransactions = await _grouper.GroupAsync(chainContext, transactions);

            var parallelize = groupedTransactions.Parallelizables;
            if (_evilTriggerOptions.ConflictTransaction && parallelize.Count >= 2 &&
                (chainContext.BlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                Logger.LogWarning(
                    $"EVIL TRIGGER - Error Parallelizables list, before evil:\n" +
                    $"{groupedTransactions.Parallelizables.First().First()},\n" +
                    $"{groupedTransactions.Parallelizables.Last().First()}");

                var transactionList1 = groupedTransactions.Parallelizables.First();
                var transactionList2 = groupedTransactions.Parallelizables.Last();
                var transaction1 = transactionList1.First();
                var transaction2 = transactionList2.First();

                groupedTransactions.Parallelizables.First().RemoveAt(0);
                groupedTransactions.Parallelizables.First().Insert(0, transaction2);

                groupedTransactions.Parallelizables.Last().RemoveAt(0);
                groupedTransactions.Parallelizables.Last().Insert(0, transaction1);
                Logger.LogWarning(
                    $"EVIL TRIGGER - Error Parallelizables list, after evil:\n" +
                    $"{groupedTransactions.Parallelizables.First().First()},\n" +
                    $"{groupedTransactions.Parallelizables.Last().First()}");
            }

            var returnSets = new List<ExecutionReturnSet>();
            List<ExecutionReturnSet> conflictingSets;

            if (_systemTransactionExtraDataProvider.TryGetSystemTransactionCount(blockHeader, out _))
            {
                var mergeResult = await ExecuteParallelizableTransactionsAsync(groupedTransactions.Parallelizables,
                    blockHeader, transactionExecutingDto.PartialBlockStateSet, cancellationToken);
                returnSets.AddRange(mergeResult.ExecutionReturnSets);
                conflictingSets = mergeResult.ConflictingReturnSets;

                var returnSetCollection = new ExecutionReturnSetCollection(returnSets);
                var updatedPartialBlockStateSet = GetUpdatedBlockStateSet(returnSetCollection, transactionExecutingDto);

                var nonParallelizableReturnSets = await ExecuteNonParallelizableTransactionsAsync(
                    groupedTransactions.NonParallelizables, blockHeader,
                    updatedPartialBlockStateSet, cancellationToken);
                returnSets.AddRange(nonParallelizableReturnSets);
            }
            else
            {
                var nonParallelizableReturnSets = await ExecuteNonParallelizableTransactionsAsync(
                    groupedTransactions.NonParallelizables, blockHeader,
                    transactionExecutingDto.PartialBlockStateSet, cancellationToken);
                returnSets.AddRange(nonParallelizableReturnSets);

                var returnSetCollection = new ExecutionReturnSetCollection(returnSets);
                var updatedPartialBlockStateSet = GetUpdatedBlockStateSet(returnSetCollection, transactionExecutingDto);

                var mergeResult = await ExecuteParallelizableTransactionsAsync(groupedTransactions.Parallelizables,
                    blockHeader, updatedPartialBlockStateSet, cancellationToken);
                returnSets.AddRange(mergeResult.ExecutionReturnSets);
                conflictingSets = mergeResult.ConflictingReturnSets;
            }

            var transactionWithoutContractReturnSets = ProcessTransactionsWithoutContract(
                groupedTransactions.TransactionsWithoutContract);

            Logger.LogTrace("Merged results from transactions without contract.");
            returnSets.AddRange(transactionWithoutContractReturnSets);

            if (conflictingSets.Count > 0 &&
                returnSets.Count + conflictingSets.Count == transactionExecutingDto.Transactions.Count())
            {
                ProcessConflictingSets(conflictingSets);
                returnSets.AddRange(conflictingSets);
            }

            return returnSets;
        }

        private async Task<ExecutionReturnSetMergeResult> ExecuteParallelizableTransactionsAsync(
            List<List<Transaction>> groupedTransactions, BlockHeader blockHeader, BlockStateSet blockStateSet,
            CancellationToken cancellationToken)
        {
            var tasks = groupedTransactions.Select(
                txns => ExecuteAndPreprocessResult(new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = txns,
                    PartialBlockStateSet = blockStateSet
                }, cancellationToken));
            var results = await Task.WhenAll(tasks);
            Logger.LogTrace("Executed parallelizables.");

            var executionReturnSets = MergeResults(results, out var conflictingSets);
            Logger.LogTrace("Merged results from parallelizables.");
            return new ExecutionReturnSetMergeResult
            {
                ExecutionReturnSets = executionReturnSets,
                ConflictingReturnSets = conflictingSets
            };
        }

        private async Task<List<ExecutionReturnSet>> ExecuteNonParallelizableTransactionsAsync(
            List<Transaction> transactions,
            BlockHeader blockHeader, BlockStateSet blockStateSet, CancellationToken cancellationToken)
        {
            var nonParallelizableReturnSets = await _planTransactionExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    Transactions = transactions,
                    BlockHeader = blockHeader,
                    PartialBlockStateSet = blockStateSet
                },
                cancellationToken);

            Logger.LogTrace("Merged results from non-parallelizables.");
            return nonParallelizableReturnSets;
        }

        private List<ExecutionReturnSet> ProcessTransactionsWithoutContract(List<Transaction> transactions)
        {
            var returnSets = new List<ExecutionReturnSet>();
            foreach (var transaction in transactions)
            {
                var result = new TransactionResult
                {
                    TransactionId = transaction.GetHash(),
                    Status = TransactionResultStatus.Failed,
                    Error = "Invalid contract address."
                };
                Logger.LogDebug(result.Error);

                var returnSet = new ExecutionReturnSet
                {
                    TransactionId = result.TransactionId,
                    Status = result.Status,
                    Bloom = result.Bloom,
                    TransactionResult = result
                };
                returnSets.Add(returnSet);
            }

            return returnSets;
        }

        private void ProcessConflictingSets(List<ExecutionReturnSet> conflictingSets)
        {
            foreach (var conflictingSet in conflictingSets)
            {
                var result = new TransactionResult
                {
                    TransactionId = conflictingSet.TransactionId,
                    Status = TransactionResultStatus.Conflict,
                    Error = "Parallel conflict",
                };
                conflictingSet.Status = result.Status;
                conflictingSet.TransactionResult = result;
            }
        }

        private async Task<GroupedExecutionReturnSets> ExecuteAndPreprocessResult(
            TransactionExecutingDto transactionExecutingDto, CancellationToken cancellationToken)
        {
            var executionReturnSets =
                await _planTransactionExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken);
            var changeKeys =
                executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateDeletes.Keys));
            var allKeys = new HashSet<string>(
                executionReturnSets.SelectMany(s => s.StateAccesses.Keys));
            var readKeys = allKeys.Where(k => !changeKeys.Contains(k));

            return new GroupedExecutionReturnSets
            {
                ReturnSets = executionReturnSets,
                AllKeys = allKeys,
                ChangeKeys = changeKeys,
                ReadKeys = readKeys
            };
        }

        private class GroupedExecutionReturnSets
        {
            public List<ExecutionReturnSet> ReturnSets { get; set; }

            public HashSet<string> AllKeys { get; set; }

            public IEnumerable<string> ChangeKeys { get; set; }

            public IEnumerable<string> ReadKeys { get; set; }
        }

        private HashSet<string> GetReadOnlyKeys(GroupedExecutionReturnSets[] groupedExecutionReturnSetsArray)
        {
            var readKeys = new HashSet<string>(groupedExecutionReturnSetsArray.SelectMany(s => s.ReadKeys));
            ;
            var changeKeys = new HashSet<string>(groupedExecutionReturnSetsArray.SelectMany(s => s.ChangeKeys));
            readKeys.ExceptWith(changeKeys);
            return readKeys;
        }

        private class ExecutionReturnSetMergeResult
        {
            public List<ExecutionReturnSet> ExecutionReturnSets { get; set; }

            public List<ExecutionReturnSet> ConflictingReturnSets { get; set; }
        }

        private List<ExecutionReturnSet> MergeResults(
            GroupedExecutionReturnSets[] groupedExecutionReturnSetsArray,
            out List<ExecutionReturnSet> conflictingSets)
        {
            var returnSets = new List<ExecutionReturnSet>();
            conflictingSets = new List<ExecutionReturnSet>();
            var existingKeys = new HashSet<string>();
            var readOnlyKeys = GetReadOnlyKeys(groupedExecutionReturnSetsArray);
            foreach (var groupedExecutionReturnSets in groupedExecutionReturnSetsArray)
            {
                groupedExecutionReturnSets.AllKeys.ExceptWith(readOnlyKeys);
                if (!existingKeys.Overlaps(groupedExecutionReturnSets.AllKeys))
                {
                    returnSets.AddRange(groupedExecutionReturnSets.ReturnSets);
                    foreach (var key in groupedExecutionReturnSets.AllKeys)
                    {
                        existingKeys.Add(key);
                    }
                }
                else
                {
                    conflictingSets.AddRange(groupedExecutionReturnSets.ReturnSets);
                }
            }

            if (readOnlyKeys.Count == 0) return returnSets;

            foreach (var returnSet in returnSets.Concat(conflictingSets))
            {
                returnSet.StateAccesses.RemoveAll(k => readOnlyKeys.Contains(k.Key));
            }

            return returnSets;
        }

        private BlockStateSet GetUpdatedBlockStateSet(ExecutionReturnSetCollection executionReturnSetCollection,
            TransactionExecutingDto transactionExecutingDto)
        {
            var updatedPartialBlockStateSet = executionReturnSetCollection.ToBlockStateSet();
            if (transactionExecutingDto.PartialBlockStateSet != null)
            {
                var partialBlockStateSet = transactionExecutingDto.PartialBlockStateSet.Clone();
                foreach (var change in partialBlockStateSet.Changes)
                {
                    if (updatedPartialBlockStateSet.Changes.TryGetValue(change.Key, out _)) continue;
                    if (updatedPartialBlockStateSet.Deletes.Contains(change.Key)) continue;
                    updatedPartialBlockStateSet.Changes[change.Key] = change.Value;
                }

                foreach (var delete in partialBlockStateSet.Deletes)
                {
                    if (updatedPartialBlockStateSet.Deletes.Contains(delete)) continue;
                    if (updatedPartialBlockStateSet.Changes.TryGetValue(delete, out _)) continue;
                    updatedPartialBlockStateSet.Deletes.Add(delete);
                }
            }

            return updatedPartialBlockStateSet;
        }
    }
}