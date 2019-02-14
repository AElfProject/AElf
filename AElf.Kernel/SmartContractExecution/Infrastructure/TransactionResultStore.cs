using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public class TransactionResultStore : KeyValueStoreBase<StateKeyValueDbContext>, ITransactionResultStore
    {
        public TransactionResultStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionResultPrefix)
        {
        }
    }
}
