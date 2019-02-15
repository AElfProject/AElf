using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class BlockBodyStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, IBlockBodyStore
    {
        public BlockBodyStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.BlockBodyPrefix)
        {
        }
    }
}
