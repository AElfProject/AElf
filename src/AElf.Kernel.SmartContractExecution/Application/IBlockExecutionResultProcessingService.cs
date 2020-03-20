using System.Threading.Tasks;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockExecutionResultProcessingService
    {
        Task ProcessBlockExecutionResultAsync(BlockExecutionResult blockExecutionResult);
    }
}