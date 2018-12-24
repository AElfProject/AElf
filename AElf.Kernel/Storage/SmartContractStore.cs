using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class SmartContractStore : KeyValueStoreBase
    {
        public SmartContractStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.SmartContractPrefix)
        {
        }
    }
}
