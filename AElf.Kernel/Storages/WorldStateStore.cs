using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {

        private readonly Dictionary<Hash, IChangesCollection> _changesCollectionDict;

        public WorldStateStore(Dictionary<Hash, IChangesCollection> changesCollectionDict)
        {
            _changesCollectionDict = changesCollectionDict;
        }

        public Task InsertWorldState(Hash chainId, Hash blockHash, IChangesCollection changes)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            var changesStore = (ChangesCollection)changes.Clone();
            _changesCollectionDict[wsKey] = changesStore;
            return Task.CompletedTask;
        }

        public Task<WorldState> GetWorldState(Hash chainId, Hash blockHash)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            if (_changesCollectionDict.TryGetValue(wsKey, out var changes))
            {
                return Task.FromResult(new WorldState(changes));
            }
            
            var changesStore = new ChangesCollection();
            _changesCollectionDict[wsKey] = changesStore;
            return Task.FromResult(new WorldState(changesStore));
        }
    }
}