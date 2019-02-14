using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class TransactionStore : KeyValueStoreBase<BlockchainKeyValueDbContext>, ITransactionStore
    {
        public TransactionStore(IByteSerializer byteSerializer, BlockchainKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionPrefix)
        {
        }
    }
}
