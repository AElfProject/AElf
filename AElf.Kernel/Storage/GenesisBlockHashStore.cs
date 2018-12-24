using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class GenesisBlockHashStore : KeyValueStoreBase
    {
        public GenesisBlockHashStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.GenesisBlockHashPrefix)
        {
        }
    }
}
