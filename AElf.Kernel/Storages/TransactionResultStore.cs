using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionResultStore : KeyValueStoreBase<StateKeyValueDbContext>, ITransactionResultStore
    {
        public TransactionResultStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext, string dataPrefix) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionResultPrefix)
        {
        }
    }
}
