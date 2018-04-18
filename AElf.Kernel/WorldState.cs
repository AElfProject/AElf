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

        public WorldState(IChangesStore changesStore)
        {
            _changesStore = changesStore;
        }

        public async Task<Change> GetChange(Hash pathHash)
        {
            return await _changesStore.GetAsync(pathHash);
        }

        public async Task<List<Hash>> GetChangedPathHashesAsync()
        {
            return await _changesStore.GetChangedPathHashesAsync();
        }

        public async Task<List<Change>> GetChangesAsync()
        {
            return await _changesStore.GetChangesAsync();
        }
        
        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var changes = await _changesStore.GetChangedPathHashesAsync();
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(changes);
            return await Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
