using AElf.Common;
using AElf.Common.Serializers;

namespace AElf.Kernel.Infrastructure
{
    public class BinaryMerkleTreeStore : KeyValueStoreBase<StateKeyValueDbContext>, IBinaryMerkleTreeStore
    {
        public BinaryMerkleTreeStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext) :
            base(byteSerializer, keyValueDbContext, StorePrefix.MerkleTreePrefix)
        {
        }
    }
}