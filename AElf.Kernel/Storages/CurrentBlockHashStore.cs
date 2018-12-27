using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class CurrentBlockHashStore : KeyValueStoreBase, ICurrentBlockHashStore
    {
        public CurrentBlockHashStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.CurrentBlockHashPrefix)
        {
        }
    }
}
