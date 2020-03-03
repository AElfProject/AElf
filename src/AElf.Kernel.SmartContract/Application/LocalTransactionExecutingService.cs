using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.SmartContract.Application
{
    public class LocalTransactionExecutingService : ITransactionExecutingService
    {
        private readonly ITransactionExecutor _transactionExecutor;

        public LocalTransactionExecutingService(ITransactionExecutor transactionExecutor)
        {
            _transactionExecutor = transactionExecutor;
        }

        public Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken,
            bool throwException = false)
        {
            return _transactionExecutor.ExecuteAsync(transactionExecutingDto, cancellationToken, throwException);
        }
    }
}