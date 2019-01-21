using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, IBlockBodyStore
    {
        public BlockBodyStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.BlockBodyPrefix)
        {
        }
    }
}
