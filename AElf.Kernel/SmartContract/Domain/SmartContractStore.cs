using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class SmartContractStore : KeyValueStoreBase<StateKeyValueDbContext>, ISmartContractStore
    {
        public SmartContractStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.SmartContractPrefix)
        {
        }
    }
}
