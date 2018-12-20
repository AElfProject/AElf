using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class MerkleTreeStore : KeyValueStoreBase, IMerkleTreeStore
    {
        public MerkleTreeStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.MerkleTreePrefix)
        {
        }
    }
}