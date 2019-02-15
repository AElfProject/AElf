using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionReceiptStore : KeyValueStoreBase<StateKeyValueDbContext>,ITransactionReceiptStore
    {
        public TransactionReceiptStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionReceiptPrefix)
        {
        }
    }
}
