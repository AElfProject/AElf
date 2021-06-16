using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;
using Orleans;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public class OrleansParallelTransactionExecutingService : LocalParallelTransactionExecutingService
    {
        private readonly IClusterClient _client;
        
        public ILogger<OrleansParallelTransactionExecutingService> Logger { get; set; }
        
        public OrleansParallelTransactionExecutingService(ITransactionGrouper grouper,
            IPlainTransactionExecutingService planTransactionExecutingService,IClusterClient client)
            : base(grouper, planTransactionExecutingService)
        {
            _client = client;
            
            Logger = NullLogger<OrleansParallelTransactionExecutingService>.Instance;
        }

        protected override async Task<ExecutionReturnSetMergeResult> ExecuteParallelizableTransactionsAsync(
            List<List<Transaction>> groupedTransactions, BlockHeader blockHeader, BlockStateSet blockStateSet,
            CancellationToken cancellationToken)
        {
            if (groupedTransactions.Count == 0)
            {
                return new ExecutionReturnSetMergeResult
                {
                    ConflictingReturnSets = new List<ExecutionReturnSet>(),
                    ExecutionReturnSets = new List<ExecutionReturnSet>()
                };
            }
            
            Logger.LogTrace("Begin OrleansParallelTransactionExecutingService.ExecuteParallelizableTransactionsAsync");
            
            var resultCollection = new ConcurrentBag<GroupedExecutionReturnSets>();
            System.Threading.Tasks.Parallel.ForEach(groupedTransactions, groupedTransaction =>
            {
                AsyncHelper.RunSync(async () =>
                {
                    var grain = _client.GetGrain<ITransactionExecutingGrain>(Guid.Empty);
                    var processResult = await grain.ExecuteAsync(new TransactionExecutingDto
                    {
                        BlockHeader = blockHeader,
                        Transactions = groupedTransaction,
                        PartialBlockStateSet = blockStateSet
                    }, cancellationToken).ConfigureAwait(false);
            
                    resultCollection.Add(processResult);
                });
            });
            
            var executionReturnSets = MergeResults(resultCollection.ToArray(),out var conflictingSets);
            
            Logger.LogTrace("End OrleansParallelTransactionExecutingService.ExecuteParallelizableTransactionsAsync");
            return new ExecutionReturnSetMergeResult
            {
                ExecutionReturnSets = executionReturnSets,
                ConflictingReturnSets = conflictingSets
            };
        }
    }
}