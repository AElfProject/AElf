using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class FunctionMetadataStore : KeyValueStoreBase<StateKeyValueDbContext>, IFunctionMetadataStore
    {
        public FunctionMetadataStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.FunctionMetadataPrefix)
        {
        }
    }
}
