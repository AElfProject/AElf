using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class BlockHeaderStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, IBlockHeaderStore
    {
        public BlockHeaderStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, StorePrefix.BlockHeaderPrefix)
        {
        }
    }
}
