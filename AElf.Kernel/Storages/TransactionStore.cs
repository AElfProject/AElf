using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, ITransactionStore
    {
        public TransactionStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionPrefix)
        {
        }
    }
}
