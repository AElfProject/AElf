using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public class TransactionTraceStore : KeyValueStoreBase<StateKeyValueDbContext>, ITransactionTraceStore
    {
        public TransactionTraceStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.TransactionTracePrefix)
        {
        }
    }
}
