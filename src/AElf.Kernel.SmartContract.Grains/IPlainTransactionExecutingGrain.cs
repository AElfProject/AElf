using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Orleans;

namespace AElf.Kernel.SmartContract.Grains;

public interface IPlainTransactionExecutingGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<string>> GetTransactionPoolStatusAsync();
    
    Task<GrainResultDto<string>> ExecuteTransactionAsync(Transaction transaction);

    Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken);
}