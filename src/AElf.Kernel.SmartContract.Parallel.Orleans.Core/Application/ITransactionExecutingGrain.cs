using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using Orleans;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public interface ITransactionExecutingGrain : IGrainWithGuidKey
    {
        Task<GroupedExecutionReturnSets> ExecuteAsync(
            TransactionExecutingDto transactionExecutingDto, GrainCancellationToken cancellationToken);
    }
}