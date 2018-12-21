using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Storage.Storages
{
    public class TransactionResultStore : KeyValueStoreBase, ITransactionResultStore
    {
        public TransactionResultStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.TransactionResultPrefix)
        {
        }
    }
}
