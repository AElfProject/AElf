using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Parallel.Application;

public class LocalParallelTransactionExecutingService : IParallelTransactionExecutingService, ISingletonDependency
{
    private readonly ITransactionGrouper _grouper;
    private readonly IPlainTransactionExecutingService _planTransactionExecutingService;

    public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
        IPlainTransactionExecutingService planTransactionExecutingService,
        ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider)
    {
        _grouper = grouper;
        _planTransactionExecutingService = planTransactionExecutingService;
        EventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;
    }

    public ILogger<LocalParallelTransactionExecutingService> Logger { get; set; }
    public ILocalEventBus EventBus { get; set; }

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

        var returnSets = new List<ExecutionReturnSet>();

        var mergeResult = await ExecuteParallelizableTransactionsAsync(groupedTransactions.Parallelizables,
            blockHeader, transactionExecutingDto.PartialBlockStateSet, cancellationToken);
        returnSets.AddRange(mergeResult.ExecutionReturnSets);
        var conflictingSets = mergeResult.ConflictingReturnSets;

        var returnSetCollection = new ExecutionReturnSetCollection(returnSets);
        var updatedPartialBlockStateSet = GetUpdatedBlockStateSet(returnSetCollection, transactionExecutingDto);

        var nonParallelizableReturnSets = await ExecuteNonParallelizableTransactionsAsync(
            groupedTransactions.NonParallelizables, blockHeader,
            updatedPartialBlockStateSet, cancellationToken);
        returnSets.AddRange(nonParallelizableReturnSets);

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
        Logger.LogInformation("ExecuteParallelizableTransactionsAsync groupedTransactions size:{}",groupedTransactions.Count);
        Logger.LogInformation("ExecuteParallelizableTransactionsAsync blockStateSet size:{}",blockStateSet.BlockExecutedData.Count);

        var tasks = groupedTransactions.Select(
            txns => ExecuteAndPreprocessResult(new TransactionExecutingDto
            {
                BlockHeader = blockHeader,
                Transactions = txns,
                PartialBlockStateSet = blockStateSet
            }, cancellationToken));
        Logger.LogInformation("ExecuteParallelizableTransactionsAsync tasks size:{}",tasks.Count());

        var timeout = 200;
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
        var resultList = new List<GroupedExecutionReturnSets>();

        if (completedTask == timeoutTask)
        {
            Logger.LogInformation("Timeout reached. Handling already completed tasks.");

            var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
            foreach (var task in completedTasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    Logger.LogInformation($"Task completed with result: {task.Result}");
                    resultList.Add(task.Result);
                }
            }
        }
        else
        {
            Logger.LogInformation("All tasks completed before timeout.");
            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                Logger.LogInformation($"Task completed with result: {result}");
                resultList.Add(result);
            }
        }
        Logger.LogTrace("Executed parallelizables.");
    
        var executionReturnSets = MergeResults(resultList.ToArray(), out var conflictingSets);
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
                Error = "Parallel conflict"
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

    private HashSet<string> GetReadOnlyKeys(GroupedExecutionReturnSets[] groupedExecutionReturnSetsArray)
    {
        var readKeys = new HashSet<string>(groupedExecutionReturnSetsArray.SelectMany(s => s.ReadKeys));
        ;
        var changeKeys = new HashSet<string>(groupedExecutionReturnSetsArray.SelectMany(s => s.ChangeKeys));
        readKeys.ExceptWith(changeKeys);
        return readKeys;
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
                foreach (var key in groupedExecutionReturnSets.AllKeys) existingKeys.Add(key);
            }
            else
            {
                conflictingSets.AddRange(groupedExecutionReturnSets.ReturnSets);
            }
        }

        if (readOnlyKeys.Count == 0) return returnSets;

        foreach (var returnSet in returnSets.Concat(conflictingSets))
            returnSet.StateAccesses.RemoveAll(k => readOnlyKeys.Contains(k.Key));

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

    private class GroupedExecutionReturnSets
    {
        public List<ExecutionReturnSet> ReturnSets { get; set; }

        public HashSet<string> AllKeys { get; set; }

        public IEnumerable<string> ChangeKeys { get; set; }

        public IEnumerable<string> ReadKeys { get; set; }
    }

    private class ExecutionReturnSetMergeResult
    {
        public List<ExecutionReturnSet> ExecutionReturnSets { get; set; }

        public List<ExecutionReturnSet> ConflictingReturnSets { get; set; }
    }
}