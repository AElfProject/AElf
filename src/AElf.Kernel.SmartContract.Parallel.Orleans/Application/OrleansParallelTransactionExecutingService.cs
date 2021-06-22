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
            GroupedExecutionReturnSets[] returnSets;
            using (var grainCancellationToken = new GrainCancellationTokenSource())
            using (cancellationToken.Register(() => { AsyncHelper.RunSync(grainCancellationToken.Cancel); }))
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
                        catch (ConnectionFailedException ex)
                        {
                            Logger.LogWarning(ex, "Transaction executing grain connection failed.");
                        }
                        catch (GrainExtensionNotInstalledException ex)
                        {
                            Logger.LogWarning(ex, "Transaction executing grain not installed failed.");
                        }
                        catch (OrleansException ex)
                        {
                            Logger.LogWarning(ex, "Transaction executing grain failed.");
                        }

                        return Task.FromResult(new GroupedExecutionReturnSets
                        {
                            ReturnSets = new List<ExecutionReturnSet>(),
                            AllKeys = new HashSet<string>(),
                            ChangeKeys = new List<string>(),
                            ReadKeys = new List<string>()
                        });
                    });

                returnSets = await Task.WhenAll(tasks).ConfigureAwait(false);
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