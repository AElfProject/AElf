using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : KeyValueStoreBase, IBlockBodyStore
    {
        public BlockBodyStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.BlockBodyPrefix)
        {
        }
    }
}
