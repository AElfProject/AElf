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
        private readonly IChangesCollection _changesCollection;

        public WorldState(IChangesCollection changesCollection)
        {
            _changesCollection = changesCollection;
        }

        public async Task<Change> GetChange(Hash pathHash)
        {
            return await _changesCollection.GetAsync(pathHash);
        }

        public async Task<List<Hash>> GetChangedPathHashesAsync()
        {
            return await _changesCollection.GetChangedPathHashesAsync();
        }

        public async Task<List<Change>> GetChangesAsync()
        {
            return await _changesCollection.GetChangesAsync();
        }
        
        public async Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var changes = await _changesCollection.GetChangedPathHashesAsync();
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(changes);
            return await Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
