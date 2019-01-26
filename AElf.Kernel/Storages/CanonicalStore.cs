using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class CanonicalStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, ICanonicalStore
    {
        public CanonicalStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.CanonicalPrefix)
        {
        }
    }
}
