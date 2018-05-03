using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Merkle;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        private readonly IChangesStore _changesStore;
        private readonly IEnumerable<Hash> _merkleTreeNodes;

        public WorldState(IChangesStore changesStore, IEnumerable<Hash> merkleTreeNodes)
        {
            _changesStore = changesStore;
            _merkleTreeNodes = merkleTreeNodes;
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            return await _changesStore.GetAsync(pathHash);
        }

        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(_merkleTreeNodes);
            return await Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
