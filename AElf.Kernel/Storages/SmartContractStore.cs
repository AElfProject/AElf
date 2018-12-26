using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class SmartContractStore : KeyValueStoreBase, ISmartContractStore
    {
        public SmartContractStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.SmartContractPrefix)
        {
        }
    }
}
