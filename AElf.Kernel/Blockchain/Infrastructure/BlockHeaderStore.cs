using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockHeaderStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, IBlockHeaderStore
    {
        public BlockHeaderStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.BlockHeaderPrefix)
        {
        }
    }
}
