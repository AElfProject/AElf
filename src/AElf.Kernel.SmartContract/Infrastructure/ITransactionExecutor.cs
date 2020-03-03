using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ITransactionExecutor
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException = false);
    }
}