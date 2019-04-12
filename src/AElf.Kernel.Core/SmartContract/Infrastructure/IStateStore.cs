using AElf.Kernel.Infrastructure;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IStateStore<T> : IKeyValueStore<T>
        where T : IMessage<T>, new()
    {
    }
}