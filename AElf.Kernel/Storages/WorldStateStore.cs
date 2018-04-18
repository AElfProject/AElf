using System;
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

        public async Task InsertWorldState(Hash chainId, Hash blockHash, IChangesStore changes)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            await _keyValueDatabase.SetAsync(wsKey, changes);
        }

        public async Task<WorldState> GetWorldState(Hash chainId, Hash blockHash)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            var changesCollection = (ChangesStore) await _keyValueDatabase.GetAsync(wsKey, typeof(ChangesStore));
            return await Task.FromResult(new WorldState(changesCollection));
        }
    }
}