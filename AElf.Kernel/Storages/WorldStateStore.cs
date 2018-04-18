using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly KeyValueDatabase _keyValueDatabase;
        
        public WorldStateStore(KeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertWorldState(Hash chainId, Hash blockHash, IChangesCollection changes)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            var changesCollection = (ChangesCollection)changes.Clone();
            await _keyValueDatabase.SetAsync(wsKey, changesCollection);
        }

        public async Task<WorldState> GetWorldState(Hash chainId, Hash blockHash)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            var changesCollection = (ChangesCollection) await _keyValueDatabase.GetAsync(wsKey, typeof(ChangesCollection));
            return await Task.FromResult(new WorldState(changesCollection));
        }
    }
}