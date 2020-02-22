using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: the inherit is very very strange
    public interface ILocalTransactionExecutingService: ILocalParallelTransactionExecutingService
    {
    }
    public interface ILocalParallelTransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException = false);
    }
}