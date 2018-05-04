using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public partial class WorldState : IWorldState
    {
        public WorldState(ChangesDict changesDict)
        {
            changesDict_ = changesDict;
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            return await Task.FromResult(changesDict_.Dict.First(i => i.Key == pathHash).Value);
        }

        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var merkleTree = new BinaryMerkleTree();
            foreach (var pair in changesDict_.Dict)
            {
                merkleTree.AddNode(pair.Key);
            }

            return await Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
