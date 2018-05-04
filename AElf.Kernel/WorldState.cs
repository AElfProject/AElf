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
