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
        private readonly List<Change> _changes = new List<Change>();
        private readonly IChangesStore _changesStore;

        public WorldState(IChangesStore changesStore)
        {
            _changesStore = changesStore;
        }

        public async Task ChangePointer(Path pointer, Hash blockHash)
        {
            var change = new Change
            {
                Before = pointer.GetPointerHash(),
                After = pointer.SetBlockHash(blockHash).GetPointerHash()
            };
            _changes.Add(change);

            var path = pointer.SetBlockHashToNull();
            await _changesStore.InsertAsync(path.GetPathHash(), change);
        }

        public async Task<Change> GetChange(Hash pathHash)
        {
            return await _changesStore.GetAsync(pathHash);
        }
        
        public Task<Hash> GetWorldStateMerkleTreeRootAsync()
        {
            var pointerHashThatCanged = _changes.Select(ch => ch.Before);
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(pointerHashThatCanged);
            return Task.FromResult(merkleTree.ComputeRootHash());
        }
    }
}
