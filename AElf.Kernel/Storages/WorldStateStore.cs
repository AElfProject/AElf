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

        //TODO: value should be WorldState which can be serialized.
        public async Task InsertWorldState(Hash chainId, Hash blockHash, IChangesStore changesStore)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            await _keyValueDatabase.SetAsync(wsKey, changesStore);
        }

        //TODO: Same as above.
        public async Task<IChangesStore> GetWorldState(Hash chainId, Hash blockHash)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            return (IChangesStore) await _keyValueDatabase.GetAsync(wsKey, typeof(IChangesStore));
        }
    }
}