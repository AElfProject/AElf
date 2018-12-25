using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class CallGraphStore : KeyValueStoreBase, ICallGraphStore
    {
        public CallGraphStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.CallGraphPrefix)
        {
        }
    }
}
