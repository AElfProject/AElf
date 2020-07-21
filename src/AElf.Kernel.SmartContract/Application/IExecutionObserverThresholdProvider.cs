using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutionObserverThresholdProvider
    {
        IExecutionObserverThreshold GetExecutionObserverThreshold(IChainContext chainContext);
        Task SetExecutionObserverThresholdAsync(IBlockIndex blockIndex, IExecutionObserverThreshold executionObserverThreshold);
    }
}