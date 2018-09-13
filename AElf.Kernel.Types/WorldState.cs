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
            var nodes = Data.Select(p => p.StateMerkleTreeLeaf).ToList();
            nodes.Sort(BinaryMerkleTree.CompareHash);
            return await Task.FromResult(new BinaryMerkleTree().AddNodes(nodes).ComputeRootHash());
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