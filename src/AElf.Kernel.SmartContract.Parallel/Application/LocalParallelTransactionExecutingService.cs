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
        private readonly ITransactionResultService _transactionResultService;
        public ILogger<LocalParallelTransactionExecutingService> Logger
        {
            get => _logger;
            set
            {
                var transactionService = _plainExecutingService as TransactionExecutingService;
                if (transactionService != null)
                {
                    transactionService.Logger = value;
                }
                _logger = value;
            } 
        }

        private ILogger<LocalParallelTransactionExecutingService> _logger;
        public ILocalEventBus EventBus { get; set; }

        public LocalParallelTransactionExecutingService(ITransactionGrouper grouper,
            ITransactionResultService transactionResultService,
            ISmartContractExecutiveService smartContractExecutiveService, IEnumerable<IPreExecutionPlugin> prePlugins,
            IEnumerable<IPostExecutionPlugin> postPlugins)
        {
            _grouper = grouper;
            _plainExecutingService =
                new TransactionExecutingService(transactionResultService, smartContractExecutiveService, postPlugins,prePlugins
                    );
            _transactionResultService = transactionResultService;
            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<LocalParallelTransactionExecutingService>.Instance;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException = false)
        {
            Logger.LogTrace($"Entered parallel ExecuteAsync.");
            var transactions = transactionExecutingDto.Transactions.ToList();
            Logger.LogTrace($"all transaction count is {transactions.Count}");
            var blockHeader = transactionExecutingDto.BlockHeader;
            // TODO: Is it reasonable to allow throwing exception here
//            if (throwException)
//            {
//                throw new NotSupportedException(
//                    $"Throwing exception is not supported in {nameof(LocalParallelTransactionExecutingService)}.");
//            }

            var chainContext = new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash,
                BlockHeight = blockHeader.Height - 1
            };
            var groupedTransactions = await _grouper.GroupAsync(chainContext, transactions);
            var tasks = groupedTransactions.Parallelizables.Select(
                txns => ExecuteAndPreprocessResult(new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = txns,
                    PartialBlockStateSet = transactionExecutingDto.PartialBlockStateSet
                }, cancellationToken, throwException));
            var results = await Task.WhenAll(tasks);
            
            Logger.LogTrace("Executed parallelizables.");
            
            var returnSets = MergeResults(results, out var conflictingSets).Item1;
            var returnSetCollection = new ReturnSetCollection(returnSets);

            var updatedPartialBlockStateSet = returnSetCollection.ToBlockStateSet();
            if (transactionExecutingDto.PartialBlockStateSet != null)
            {
                var partialBlockStateSet = transactionExecutingDto.PartialBlockStateSet.Clone();
                foreach ( var change in partialBlockStateSet.Changes)
                {
                    if (updatedPartialBlockStateSet.Changes.TryGetValue(change.Key, out _)) continue;
                    updatedPartialBlockStateSet.Changes[change.Key] = change.Value;
                }
            }

            Logger.LogTrace("Merged results from parallelizables.");
            
            var nonParallelizableReturnSets = await _plainExecutingService.ExecuteAsync(
                new TransactionExecutingDto
                {
                    BlockHeader = blockHeader,
                    Transactions = groupedTransactions.NonParallelizables,
                    PartialBlockStateSet = updatedPartialBlockStateSet
                },
                cancellationToken, throwException);

            Logger.LogTrace("Merged results from non-parallelizables.");
            returnSets.AddRange(nonParallelizableReturnSets);
            
            var transactionWithoutContractReturnSets = await ProcessTransactionsWithoutContract(
                groupedTransactions.TransactionsWithoutContract, blockHeader);
            
            Logger.LogTrace("Merged results from transactions without contract.");
            returnSets.AddRange(transactionWithoutContractReturnSets);
            
            if (conflictingSets.Count > 0)
            {
                await EventBus.PublishAsync(new ConflictingTransactionsFoundInParallelGroupsEvent(
                    blockHeader.Height - 1,
                    blockHeader.PreviousBlockHash,
                    returnSets, conflictingSets
                ));
            }

            return returnSets;
        }
        private async Task<List<ExecutionReturnSet>> ProcessTransactionsWithoutContract(List<Transaction> transactions,
            BlockHeader blockHeader)
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
                Logger.LogError(result.Error);
                await _transactionResultService.AddTransactionResultAsync(result, blockHeader);

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

        private async Task<(List<ExecutionReturnSet>, HashSet<string>)> ExecuteAndPreprocessResult(
            TransactionExecutingDto transactionExecutingDto, CancellationToken cancellationToken,
            bool throwException = false)
        {
            if(cancellationToken.IsCancellationRequested)
                Logger.LogTrace("it should be cancelled in this method");
            try
            {
                var executionReturnSets =
                    await _plainExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken,
                        throwException).WithCancellation(cancellationToken);
                var keys = new HashSet<string>(
                    executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateAccesses.Keys)));
                return (executionReturnSets, keys);
            }
            catch
            {
                return new Tuple<List<ExecutionReturnSet>, HashSet<string>>(new List<ExecutionReturnSet>(),new HashSet<string>()).ToValueTuple();
            }
        }

        private (List<ExecutionReturnSet>, HashSet<string>) MergeResults(
            IEnumerable<(List<ExecutionReturnSet>, HashSet<string>)> results,
            out List<ExecutionReturnSet> conflictingSets)
        {
            // TODO: Throw exception upon conflicts
            var returnSets = new List<ExecutionReturnSet>();
            conflictingSets = new List<ExecutionReturnSet>();
            var existingKeys = new HashSet<string>();
            foreach (var (sets, keys) in results)
            {
                if (!existingKeys.Overlaps(keys))
                {
                    returnSets.AddRange(sets);
                    foreach (var key in keys)
                    {
                        existingKeys.Add(key);
                    }
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