using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class TransactionTraceStore : KeyValueStoreBase
    {
        public TransactionTraceStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.TransactionTracePrefix)
        {
        }
    }
}
