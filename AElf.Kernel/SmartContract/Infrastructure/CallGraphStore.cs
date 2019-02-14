using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class CallGraphStore : KeyValueStoreBase<StateKeyValueDbContext>, ICallGraphStore
    {
        public CallGraphStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.CallGraphPrefix)
        {
        }
    }
}
