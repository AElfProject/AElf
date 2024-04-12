using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Infrastructure;

public interface IStateStore<T> : IKeyValueStore<T>
    where T : IMessage<T>, new()
{
}