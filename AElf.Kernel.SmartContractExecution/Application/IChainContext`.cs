using AElf.Common;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IChainContext<T> : IChainContext where T : IStateCache
    {
        int ChainId { get; set; }
        long BlockHeight { get; set; }
        Hash BlockHash { get; set; }
        T StateCache { get; set; }
    }
}