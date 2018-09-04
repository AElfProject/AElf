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
            return await Task.FromResult(new BinaryMerkleTree().AddNodes(Data.Select(p => p.StateMerkleTreeLeaf))
                .ComputeRootHash());
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