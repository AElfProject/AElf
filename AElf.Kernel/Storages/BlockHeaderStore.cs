using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockHeaderStore : KeyValueStoreBase, IBlockHeaderStore
    {
        public BlockHeaderStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.BlockHeaderPrefix)
        {
        }
    }
}
