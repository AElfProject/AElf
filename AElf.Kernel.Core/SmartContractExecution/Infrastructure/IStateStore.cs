using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public interface IStateStore<T> : IKeyValueStore<T>
    {
    }

    public interface IStateStore : IKeyValueStore
    {
        
    }
}