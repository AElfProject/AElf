using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class ChainHeightStore : KeyValueStoreBase, IChainHeightStore
    {
        public ChainHeightStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.ChianHeightPrefix)
        {
        }
    }
}
