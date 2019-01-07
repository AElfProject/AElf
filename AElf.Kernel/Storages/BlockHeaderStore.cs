using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockHeaderStore : KeyValueStoreBase<BlockChainKeyValueDbContext>, IBlockHeaderStore
    {
        public BlockHeaderStore(IByteSerializer byteSerializer, BlockChainKeyValueDbContext keyValueDbContext, string dataPrefix) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.BlockHeaderPrefix)
        {
        }
    }
}
