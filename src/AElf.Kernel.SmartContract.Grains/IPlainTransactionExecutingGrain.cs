using AElf.Kernel.SmartContract.Application;
using Orleans;

namespace AElf.Kernel.SmartContract.Grains;

public interface IPlainTransactionExecutingGrain : IGrainWithGuidKey
{
    Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken);
}