using AElf.Common;
using AElf.Common.Serializers;

namespace AElf.Kernel.Infrastructure
{
    public class MinersStore : KeyValueStoreBase<StateKeyValueDbContext>, IMinersStore
    {
        public MinersStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.MinersPrefix)
        {
        }
    }
}
