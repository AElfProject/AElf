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
using Orleans.Runtime;
using Orleans.Runtime.Messaging;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public class OrleansParallelTransactionExecutingService : LocalParallelTransactionExecutingService
    {
        private readonly IClusterClient _client;

        public new ILogger<OrleansParallelTransactionExecutingService> Logger { get; set; }

        public OrleansParallelTransactionExecutingService(ITransactionGrouper grouper,
            IPlainTransactionExecutingService planTransactionExecutingService, IClusterClient client)
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
            GroupedExecutionReturnSets[] returnSets = new GroupedExecutionReturnSets[0];
            var grainCancellationToken = new GrainCancellationTokenSource();
            try
            {
                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        AsyncHelper.RunSync(grainCancellationToken.Cancel);
                    }
                    catch (OrleansException e)
                    {
                        Logger.LogWarning(e, "Cancel grain executing failed.");
                    }
                }))
                {
                    var tasks = groupedTransactions.Select(
                        txns =>
                        {
                            try
                            {
                                var grain = _client.GetGrain<ITransactionExecutingGrain>(Guid.NewGuid());
                                return grain.ExecuteAsync(new TransactionExecutingDto
                                {
                                    BlockHeader = blockHeader,
                                    Transactions = txns,
                                    PartialBlockStateSet = blockStateSet
                                }, grainCancellationToken.Token);
                            }
                            catch (OrleansException ex)
                            {
                                Logger.LogWarning(ex, "Grain execute failed.");
                            }

                            return Task.FromResult(new GroupedExecutionReturnSets
                            {
                                ReturnSets = new List<ExecutionReturnSet>(),
                                AllKeys = new HashSet<string>(),
                                ChangeKeys = new List<string>(),
                                ReadKeys = new List<string>()
                            });
                        }).ToList();

                    returnSets = await Task.WhenAll(tasks);
                }
            }
            catch (OrleansException ex)
            {
                Logger.LogWarning(ex, "Orleans execute failed.");
            }

            var executionReturnSets = MergeResults(returnSets, out var conflictingSets);

            Logger.LogTrace(
                "End OrleansParallelTransactionExecutingService.ExecuteParallelizableTransactionsAsync");
            return new ExecutionReturnSetMergeResult
            {
                ExecutionReturnSets = executionReturnSets,
                ConflictingReturnSets = conflictingSets
            };
        }
    }
}