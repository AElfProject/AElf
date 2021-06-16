using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    [StatelessWorker]
    public class TransactionExecutingGrain : Grain, ITransactionExecutingGrain,ISingletonDependency
    {
        private readonly IPlainTransactionExecutingService _planTransactionExecutingService;

        private ILogger<TransactionExecutingGrain> _logger;
        
        public TransactionExecutingGrain(IPlainTransactionExecutingService planTransactionExecutingService, ILogger<TransactionExecutingGrain> logger)
        {
            _planTransactionExecutingService = planTransactionExecutingService;
            _logger = logger;
        }

        public async Task<GroupedExecutionReturnSets> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Begin TransactionExecutingGrain.ExecuteAsync");
            var executionReturnSets =
                await _planTransactionExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken);
            var changeKeys =
                executionReturnSets.SelectMany(s => s.StateChanges.Keys.Concat(s.StateDeletes.Keys));
            var allKeys = new HashSet<string>(
                executionReturnSets.SelectMany(s => s.StateAccesses.Keys));
            var readKeys = allKeys.Where(k => !changeKeys.Contains(k));

            _logger.LogTrace("End TransactionExecutingGrain.ExecuteAsync");
            return new GroupedExecutionReturnSets
            {
                ReturnSets = executionReturnSets,
                AllKeys = allKeys,
                ChangeKeys = changeKeys.ToList(),
                ReadKeys = readKeys.ToList()
            };
        }
    }
}