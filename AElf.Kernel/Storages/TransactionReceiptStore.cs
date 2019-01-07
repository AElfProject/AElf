using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionReceiptStore : KeyValueStoreBase<StateKeyValueDbContext>,ITransactionReceiptStore
    {
        public TransactionReceiptStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionReceiptPrefix)
        {
        }
    }
}
