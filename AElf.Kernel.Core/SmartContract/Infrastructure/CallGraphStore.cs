using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class CallGraphStore : KeyValueStoreBase<StateKeyValueDbContext>, ICallGraphStore
    {
        public CallGraphStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, StorePrefix.CallGraphPrefix)
        {
        }
    }
}
