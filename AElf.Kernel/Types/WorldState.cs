using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Merkle;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        private readonly ChangesDict _changesDict;
        
        public WorldState(ChangesDict changesDict)
        {
            _changesDict = changesDict;
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            return await Task.FromResult(_changesDict.Dict.FirstOrDefault(i => i.Key == pathHash)?.Value);
        }

        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var merkleTree = new BinaryMerkleTree();
            foreach (var pair in _changesDict.Dict)
            {
                merkleTree.AddNode(pair.Key);
            }

            return await Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
