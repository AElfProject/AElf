using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class ChainHeightStore : KeyValueStoreBase<StateKeyValueDbContext>, IChainHeightStore
    {
        public ChainHeightStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext, string dataPrefix) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.ChianHeightPrefix)
        {
        }
    }
}
