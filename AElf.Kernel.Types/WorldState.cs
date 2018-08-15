using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Types.Merkle;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class WorldState : IWorldState
    {
        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(Data.Select(p => p.MerkleTreeLeaf));
            return await Task.FromResult(merkleTree.ComputeRootHash());
        }

        public Hash GetPointerHash(Hash pathHash)
        {
            return (from d in Data
                where d.ResourcePath == pathHash
                select d.ResourcePointer).First();
        }

        public IEnumerable<DataItem> GetContext()
        {
            return Data;
        }
    }
}
