using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class BlockHeaderStore : KeyValueStoreBase
    {
        public BlockHeaderStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.BlockHeaderPrefix)
        {
        }
    }
}
