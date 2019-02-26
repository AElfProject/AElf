using AElf.Kernel.Infrastructure;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public interface IStateStore<T> : IKeyValueStore<T>
        where T : IMessage<T>, new()
    {
    }
}