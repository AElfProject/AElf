using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    //TODO: should store in the state of chain
    public class GenesisBlockHashStore : KeyValueStoreBase<StateKeyValueDbContext>, IGenesisBlockHashStore
    {
        public GenesisBlockHashStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) 
            : base(byteSerializer, keyValueDbContext, GlobalConfig.GenesisBlockHashPrefix)
        {
        }
    }
}
