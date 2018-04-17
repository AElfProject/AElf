using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        private readonly Dictionary<Hash, IChangesCollection> _changesStoreCollection;

        public WorldStateStore(IKeyValueDatabase keyValueDatabase, Dictionary<Hash, IChangesCollection> changesStoreCollection)
        {
            _keyValueDatabase = keyValueDatabase;
            _changesStoreCollection = changesStoreCollection;
        }

        public async Task SetData(Hash pointerHash, byte[] data)
        {
            await _keyValueDatabase.SetAsync(pointerHash, data);
        }

        public async Task<byte[]> GetData(Hash pointerHash)
        {
            return (byte[]) await _keyValueDatabase.GetAsync(pointerHash, typeof(byte[]));
        }

        public Task InsertWorldState(Hash chainId, Hash blockHash, IChangesCollection changes)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            var changesStore = (ChangesCollection)changes.Clone();
            _changesStoreCollection[wsKey] = changesStore;
            return Task.CompletedTask;
        }

        public Task<WorldState> GetWorldState(Hash chainId, Hash blockHash)
        {
            var wsKey = new Hash(chainId.CalculateHashWith(blockHash));
            if (_changesStoreCollection.TryGetValue(wsKey, out var changes))
            {
                return Task.FromResult(new WorldState(changes));
            }
            
            var changesStore = new ChangesCollection();
            _changesStoreCollection[wsKey] = changesStore;
            return Task.FromResult(new WorldState(changesStore));
        }
    }
}