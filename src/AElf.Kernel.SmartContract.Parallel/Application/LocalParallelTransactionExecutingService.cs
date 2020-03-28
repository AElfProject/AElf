using System.Collections.Generic;
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
    public class LocalParallelTransactionExecutingService : IParallelTransactionExecutingService, ISingletonDependency
    {
        private readonly ITransactionGrouper _grouper;
        private readonly IPlainTransactionExecutingService _planTransactionExecutingService;
        public ILogger<LocalParallelTransactionExecutingService> Logger { get; set; }
        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            IPlainTransactionExecutingService planTransactionExecutingService)
        {
            _grouper = grouper;
            _planTransactionExecutingService = planTransactionExecutingService;
            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;
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

            var returnSets = new List<ExecutionReturnSet>();
            var nonParallelizableReturnSets = await _planTransactionExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = groupedTransactions.NonParallelizables,
                    PartialBlockStateSet = transactionExecutingDto.PartialBlockStateSet
                },
                cancellationToken);

            Logger.LogTrace("Merged results from non-parallelizables.");
            returnSets.AddRange(nonParallelizableReturnSets);

            var returnSetCollection = new ReturnSetCollection(returnSets);
            var updatedPartialBlockStateSet = GetUpdatedBlockStateSet(returnSetCollection, transactionExecutingDto);

            var tasks = groupedTransactions.Parallelizables.Select(
                txns => ExecuteAndPreprocessResult(new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = txns,
                    PartialBlockStateSet = updatedPartialBlockStateSet,
                }, cancellationToken));
            var results = await Task.WhenAll(tasks);
            Logger.LogTrace("Executed parallelizables.");

            returnSets.AddRange(MergeResults(results, out var conflictingSets));
            Logger.LogTrace("Merged results from parallelizables.");

            var transactionWithoutContractReturnSets = await ProcessTransactionsWithoutContract(
                groupedTransactions.TransactionsWithoutContract, blockHeader);

            Logger.LogTrace("Merged results from transactions without contract.");
            returnSets.AddRange(transactionWithoutContractReturnSets);

            if (conflictingSets.Count > 0 &&
                returnSets.Count + conflictingSets.Count == transactionExecutingDto.Transactions.Count())
            {
                await ProcessConflictingSetsAsync(conflictingSets, blockHeader);
                returnSets.AddRange(conflictingSets);
            }

            return returnSets;
        }

        private async Task<List<ExecutionReturnSet>> ProcessTransactionsWithoutContract(List<Transaction> transactions,
            BlockHeader blockHeader)
        {
            var transactionResults = new List<TransactionResult>();
            var returnSets = new List<ExecutionReturnSet>();
            foreach (var transaction in transactions)
            {
                var result = new TransactionResult
                {
                    TransactionId = transaction.GetHash(),
                    Status = TransactionResultStatus.Failed,
                    Error = "Invalid contract address."
                };
                Logger.LogError(result.Error);
                transactionResults.Add(result);

                var returnSet = new ExecutionReturnSet
                {
                    TransactionId = result.TransactionId,
                    Status = result.Status,
                    Bloom = result.Bloom
                };
                returnSets.Add(returnSet);
            }
            
            return returnSets;
        }

        private async Task ProcessConflictingSetsAsync(List<ExecutionReturnSet> conflictingSets,
            BlockHeader blockHeader)
        {
            var transactionResults = new List<TransactionResult>();
            foreach (var conflictingSet in conflictingSets)
            {
                var result = new TransactionResult
                {
                    TransactionId = conflictingSet.TransactionId,
                    Status = TransactionResultStatus.Conflict,
                    Error = "Parallel conflict",
                };
                conflictingSet.Status = result.Status;
                transactionResults.Add(result);
            }

        }

        private async Task<GroupedExecutionReturnSets> ExecuteAndPreprocessResult(
            TransactionExecutingDto transactionExecutingDto, CancellationToken cancellationToken)
        {
            var executionReturnSets =
                await _planTransactionExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken);
            var keys = new HashSet<string>(
                executionReturnSets.SelectMany(s =>
                    s.StateChanges.Keys.Concat(s.StateDeletes.Keys).Concat(s.StateAccesses.Keys)));
            return new GroupedExecutionReturnSets
            {
                ReturnSets = executionReturnSets,
                Keys = keys
            };
        }

        private class GroupedExecutionReturnSets
        {
            public List<ExecutionReturnSet> ReturnSets { get; set; }

            public HashSet<string> Keys { get; set; }
        }

        private List<ExecutionReturnSet> MergeResults(
            IEnumerable<GroupedExecutionReturnSets> groupedExecutionReturnSetsList,
            out List<ExecutionReturnSet> conflictingSets)
        {
            var returnSets = new List<ExecutionReturnSet>();
            conflictingSets = new List<ExecutionReturnSet>();
            var existingKeys = new HashSet<string>();
            foreach (var groupedExecutionReturnSets in groupedExecutionReturnSetsList)
            {
                if (!existingKeys.Overlaps(groupedExecutionReturnSets.Keys))
                {
                    returnSets.AddRange(groupedExecutionReturnSets.ReturnSets);
                    foreach (var key in groupedExecutionReturnSets.Keys)
                    {
                        existingKeys.Add(key);
                    }
                }
                else
                {
                    conflictingSets.AddRange(groupedExecutionReturnSets.ReturnSets);
                }
            }

            return returnSets;
        }

        private BlockStateSet GetUpdatedBlockStateSet(ReturnSetCollection returnSetCollection,
            TransactionExecutingDto transactionExecutingDto)
        {
            var updatedPartialBlockStateSet = returnSetCollection.ToBlockStateSet();
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