using System.Threading.Tasks;
using AElf.Database;

using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.ChangesDict;

        
        public WorldStateStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertWorldStateAsync(Hash chainId, Hash blockHash, ChangesDict changes)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var key = wsKey.GetKeyString(TypeIndex);     
            await _keyValueDatabase.SetAsync(key, changes.Serialize());
        }

        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var key = wsKey.GetKeyString(TypeIndex); 
            var changes = await _keyValueDatabase.GetAsync(key);
            var changesDict = changes == null ?  new ChangesDict() : ChangesDict.Parser.ParseFrom(changes);
            return new WorldState(changesDict);
        }
    }
}