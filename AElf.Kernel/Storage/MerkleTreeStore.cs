using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class MerkleTreeStore : KeyValueStoreBase
    {
        public MerkleTreeStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.MerkleTreePrefix)
        {
        }
    }
}