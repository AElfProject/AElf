using System.Threading.Tasks;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockExecutionResultProcessingService
    {
        Task ProcessBlockExecutionResultAsync(Chain chain, BlockExecutionResult blockExecutionResult);
    }
}