using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class FunctionMetadataStore : KeyValueStoreBase<StateKeyValueDbContext>, IFunctionMetadataStore
    {
        public FunctionMetadataStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.FunctionMetadataPrefix)
        {
        }
    }
}
