using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class ChainHeightStore : KeyValueStoreBase
    {
        public ChainHeightStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.ChianHeightPrefix)
        {
        }
    }
}
