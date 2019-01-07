using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class FunctionMetadataStore : KeyValueStoreBase<StateKeyValueDbContext>, IFunctionMetadataStore
    {
        public FunctionMetadataStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext, string dataPrefix)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.FunctionMetadataPrefix)
        {
        }
    }
}
