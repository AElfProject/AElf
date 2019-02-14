using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BinaryMerkleTreeStore : KeyValueStoreBase<StateKeyValueDbContext>, IBinaryMerkleTreeStore
    {
        public BinaryMerkleTreeStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) :
            base(byteSerializer, keyValueDbContext, GlobalConfig.MerkleTreePrefix)
        {
        }
    }
}