using AElf.Kernel.Infrastructure;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
        where T : IMessage<T>, new()
    {
        public StateStore(StateKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider) : base(
            keyValueDbContext, prefixProvider)
        {
        }
    }
}