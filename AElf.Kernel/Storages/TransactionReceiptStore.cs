using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionReceiptStore : KeyValueStoreBase,ITransactionReceiptStore
    {
        public TransactionReceiptStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.TransactionReceiptPrefix)
        {
        }
    }
}
