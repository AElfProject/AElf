using AElf.Common;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IChainContext<T> : IChainContext where T : IStateCache
    {
        new T StateCache { get; set; }
    }
}