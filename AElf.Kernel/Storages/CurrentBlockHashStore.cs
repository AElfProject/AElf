using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    //TODO: change the implement
    public class CurrentBlockHashStore : KeyValueStoreBase<StateKeyValueDbContext>, ICurrentBlockHashStore
    {
        public CurrentBlockHashStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.CurrentBlockHashPrefix)
        {
        }
    }
}
