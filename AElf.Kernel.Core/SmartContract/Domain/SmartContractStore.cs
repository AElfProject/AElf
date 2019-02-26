using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Domain
{
    public class SmartContractStore : KeyValueStoreBase<StateKeyValueDbContext>, ISmartContractStore
    {
        public SmartContractStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, StorePrefix.SmartContractPrefix)
        {
        }
    }
}
