using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel
{
    public class LocalParallelTransactionExecutingService : ILocalParallelTransactionExecutingService
    {
        private readonly IGrouper _grouper;
        private readonly ITransactionExecutingService _plainExecutingService;

        public LocalParallelTransactionExecutingService(IGrouper grouper,
            ITransactionExecutingService plainExecutingService)
        {
            _grouper = grouper;
            _plainExecutingService = plainExecutingService;
        }

        public async Task<List<ExecutionReturnSet>> ExecuteAsync(BlockHeader blockHeader,
            List<Transaction> transactions, CancellationToken cancellationToken, bool throwException = false,
            BlockStateSet partialBlockStateSet = null)
        {
            if (throwException)
            {
                throw new NotSupportedException(
                    $"Throwing exception is not supported in {nameof(LocalParallelTransactionExecutingService)}.");
            }

            // TODO: Group transactions
            var groups = new List<List<Transaction>>();
            var tasks = groups.Select(txns =>
                _plainExecutingService.ExecuteAsync(blockHeader, txns, cancellationToken, throwException,
                    partialBlockStateSet));
            var results = await Task.WhenAll(tasks);
            // TODO: Compare not conflicts in returned data
            return results.SelectMany(r => r).ToList();
        }
    }
}