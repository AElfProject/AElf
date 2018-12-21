using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Storage.Storages
{
    public class SmartContractStore : KeyValueStoreBase, ISmartContractStore
    {
        public SmartContractStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.SmartContractPrefix)
        {
        }
    }
}
