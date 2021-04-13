using System.Collections.Concurrent;
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
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class LocalParallelTransactionExecutingService : IParallelTransactionExecutingService, ISingletonDependency
    {
        private readonly ITransactionGrouper _grouper;
        private readonly IPlainTransactionExecutingService _planTransactionExecutingService;
        public ILogger<LocalParallelTransactionExecutingService> Logger { get; set; }
        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            IPlainTransactionExecutingService planTransactionExecutingService, 
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider)
        {
            _grouper = grouper;
            _planTransactionExecutingService = planTransactionExecutingService;
            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.ExecuteAsync");
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
            
            var returnSetCollection = new ExecutionReturnSetCollection(returnSets);
            var updatedPartialBlockStateSet = GetUpdatedBlockStateSet(returnSetCollection, transactionExecutingDto);
            
            var nonParallelizableReturnSets = await ExecuteNonParallelizableTransactionsAsync(groupedTransactions.NonParallelizables, blockHeader,
                updatedPartialBlockStateSet, cancellationToken);
            returnSets.AddRange(nonParallelizableReturnSets);
        
            var transactionWithoutContractReturnSets = ProcessTransactionsWithoutContract(
                groupedTransactions.TransactionsWithoutContract);

            returnSets.AddRange(transactionWithoutContractReturnSets);
            Logger.LogTrace("End LocalParallelTransactionExecutingService.ExecuteAsync");
            return returnSets;
        }

        private async Task<ExecutionReturnSetMergeResult> ExecuteParallelizableTransactionsAsync(
            List<List<Transaction>> groupedTransactions, BlockHeader blockHeader, BlockStateSet blockStateSet,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.ExecuteParallelizableTransactionsAsync");
            // var tasks = groupedTransactions.Select(
            //     txns => ExecuteAndPreprocessResult(new TransactionExecutingDto
            //     {
            //         BlockHeader = blockHeader,
            //         Transactions = txns,
            //         PartialBlockStateSet = blockStateSet
            //     }, cancellationToken));
            // var results = await Task.WhenAll(tasks);
            
            var resultCollection = new ConcurrentBag<GroupedExecutionReturnSets>();
            
            System.Threading.Tasks.Parallel.ForEach(groupedTransactions, groupedTransaction =>
                {
                    AsyncHelper.RunSync(async () =>
                    {
                        var processResult = await ExecuteAndPreprocessResult(new TransactionExecutingDto
                        {
                            BlockHeader = blockHeader,
                            Transactions = groupedTransaction,
                            PartialBlockStateSet = blockStateSet
                        }, cancellationToken).ConfigureAwait(false);
            
                        resultCollection.Add(processResult);
                    });
                });
            
            var executionReturnSets = MergeResults(resultCollection.ToList());
            
            Logger.LogTrace("End LocalParallelTransactionExecutingService.ExecuteParallelizableTransactionsAsync");

            return new ExecutionReturnSetMergeResult
            {
                ExecutionReturnSets = executionReturnSets
            };
        }

        private async Task<List<ExecutionReturnSet>> ExecuteNonParallelizableTransactionsAsync(List<Transaction> transactions,
            BlockHeader blockHeader, BlockStateSet blockStateSet, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.ExecuteNonParallelizableTransactionsAsync");
            var nonParallelizableReturnSets = await _planTransactionExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    Transactions = transactions,
                    BlockHeader = blockHeader,
                    PartialBlockStateSet = blockStateSet
                }, 
                cancellationToken);
        
            Logger.LogTrace("End LocalParallelTransactionExecutingService.ExecuteNonParallelizableTransactionsAsync");
            return nonParallelizableReturnSets;
        }

        private List<ExecutionReturnSet> ProcessTransactionsWithoutContract(List<Transaction> transactions)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.ProcessTransactionsWithoutContract");
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
            Logger.LogTrace("End LocalParallelTransactionExecutingService.ProcessTransactionsWithoutContract");
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
            // var changeKeys =
            //         executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateDeletes.Keys));
            // var allKeys = new HashSet<string>(
            //     executionReturnSets.SelectMany(s =>s.StateAccesses.Keys));
            // var readKeys = allKeys.Where(k => !changeKeys.Contains(k));

            var allKeys = new HashSet<string>();
            var changeKeys = new List<string>();

            foreach (var executionReturnSet in executionReturnSets)
            {
                foreach (var stateAccess in executionReturnSet.StateAccesses)
                {
                    allKeys.Add(stateAccess.Key);
                }
                
                changeKeys.AddRange(executionReturnSet.StateChanges.Keys);
                changeKeys.AddRange(executionReturnSet.StateDeletes.Keys);
            }
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
            var readKeys = new HashSet<string>(groupedExecutionReturnSetsArray.SelectMany(s => s.ReadKeys));;
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
            List<GroupedExecutionReturnSets> groupedExecutionReturnSetsArray)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.MergeResults");
            var returnSets = new List<ExecutionReturnSet>();
            foreach (var groupedExecutionReturnSets in groupedExecutionReturnSetsArray)
            {
                returnSets.AddRange(groupedExecutionReturnSets.ReturnSets);
            }
            Logger.LogTrace("End LocalParallelTransactionExecutingService.MergeResults");
            return returnSets;
        }

        private BlockStateSet GetUpdatedBlockStateSet(ExecutionReturnSetCollection executionReturnSetCollection,
            TransactionExecutingDto transactionExecutingDto)
        {
            Logger.LogTrace("Begin LocalParallelTransactionExecutingService.GetUpdatedBlockStateSet");
            var updatedPartialBlockStateSet = executionReturnSetCollection.ToBlockStateSet();
            if (transactionExecutingDto.PartialBlockStateSet != null)
            {
                var partialBlockStateSet = transactionExecutingDto.PartialBlockStateSet.Clone();
                Logger.LogTrace("Handle PartialBlockStateSet Changes");
                foreach (var change in partialBlockStateSet.Changes)
                {
                    if (updatedPartialBlockStateSet.Changes.TryGetValue(change.Key, out _)) continue;
                    if (updatedPartialBlockStateSet.Deletes.Contains(change.Key)) continue;
                    updatedPartialBlockStateSet.Changes[change.Key] = change.Value;
                }
                Logger.LogTrace("Handle PartialBlockStateSet Deletes");
                foreach (var delete in partialBlockStateSet.Deletes)
                {
                    if (updatedPartialBlockStateSet.Deletes.Contains(delete)) continue;
                    if (updatedPartialBlockStateSet.Changes.TryGetValue(delete, out _)) continue;
                    updatedPartialBlockStateSet.Deletes.Add(delete);
                }
            }
            Logger.LogTrace("End LocalParallelTransactionExecutingService.GetUpdatedBlockStateSet");
            return updatedPartialBlockStateSet;
        }
    }
}