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

        public async Task InsertWorldStateAsync(Hash chainId, Hash blockHash, ChangesDict changes)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            await _keyValueDatabase.SetAsync(wsKey, changes);
        }

        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var changesDict = (ChangesDict) await _keyValueDatabase.GetAsync(wsKey, typeof(ChangesDict));
            return new WorldState(changesDict);
        }
    }
}