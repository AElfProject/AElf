using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        private readonly Dictionary<Hash, IChangesStore> _changesStoreCollection;

        public WorldStateStore(IKeyValueDatabase keyValueDatabase, Dictionary<Hash, IChangesStore> changesStoreCollection)
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

        public void InsertWorldState(Hash chainId, Hash blockHash, IChangesStore changes)
        {
            _changesStoreCollection.Add(new Hash(chainId.CalculateHashWith(blockHash)), changes);
        }

        public WorldState GetWorldState(Hash chainId, Hash blockHash)
        {
            var key = new Hash(chainId.CalculateHashWith(blockHash));
            if (_changesStoreCollection.TryGetValue(key, out var changes))
            {
                return new WorldState(changes);
            }
            
            var changesStore = new ChangesStore(new KeyValueDatabase());
            _changesStoreCollection[key] = changesStore;
            return new WorldState(changesStore);
        }
    }
}