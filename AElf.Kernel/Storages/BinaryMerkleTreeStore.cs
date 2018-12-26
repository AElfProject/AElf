using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BinaryMerkleTreeStore : KeyValueStoreBase, IBinaryMerkleTreeStore
    {
        public BinaryMerkleTreeStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.MerkleTreePrefix)
        {
        }
    }
}