using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        private readonly Dictionary<Hash, IChangesStore> _worldStates;

        public WorldStateStore(IKeyValueDatabase keyValueDatabase, Dictionary<Hash, IChangesStore> worldStates)
        {
            _keyValueDatabase = keyValueDatabase;
            _worldStates = worldStates;
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
            _worldStates.Add(new Hash(chainId.CalculateHashWith(blockHash)), changes);
        }

        public WorldState GetWorldState(Hash chainId, Hash blockHash)
        {
            if (_worldStates.TryGetValue(new Hash(chainId.CalculateHashWith(blockHash)), out var changes))
            {
                return new WorldState(changes);
            }
            
            throw new InvalidOperationException();
        }
    }
}