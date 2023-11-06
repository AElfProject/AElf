using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Grains;
using AElf.Types;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IPlainTransactionExecutingGrain : IGrainWithGuidKey
{
    Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken);
}