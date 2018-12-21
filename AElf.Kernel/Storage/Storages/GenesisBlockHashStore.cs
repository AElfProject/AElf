using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Storage.Storages
{
    public class GenesisBlockHashStore : KeyValueStoreBase, IGenesisBlockHashStore
    {
        public GenesisBlockHashStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.GenesisBlockHashPrefix)
        {
        }
    }
}
