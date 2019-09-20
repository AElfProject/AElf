using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
//    public interface ITransactionExecutingService
//    {
//        Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
//            CancellationToken cancellationToken, bool throwException = false);
//    }
    public interface ILocalTransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException = false);
    }
    public interface ILocalParallelTransactionExecutingService
    {
        Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
            CancellationToken cancellationToken, bool throwException = false);
    }
}