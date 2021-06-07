using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public class TransactionExecutingGrain : Grain, ITransactionExecutingGrain,ISingletonDependency
    {
        private readonly IPlainTransactionExecutingService _planTransactionExecutingService;

        public TransactionExecutingGrain(IPlainTransactionExecutingService planTransactionExecutingService)
        {
            _planTransactionExecutingService = planTransactionExecutingService;
        }

        public async Task<GroupedExecutionReturnSets> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken)
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
    }
}