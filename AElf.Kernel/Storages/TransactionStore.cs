using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionStore : KeyValueStoreBase<BlockChainKeyValueDbContext>, ITransactionStore
    {
        public TransactionStore(IByteSerializer byteSerializer, BlockChainKeyValueDbContext keyValueDbContext, string dataPrefix) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionPrefix)
        {
        }
    }
}
