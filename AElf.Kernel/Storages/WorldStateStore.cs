using System.Threading.Tasks;
using AElf.Database;

using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        public WorldStateStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertWorldStateAsync(Hash chainId, Hash blockHash, ChangesDict changes)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var key = wsKey.GetKeyString(TypeName.TnChangesDict);     
            await _keyValueDatabase.SetAsync(key, changes.Serialize());
        }

        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var key = wsKey.GetKeyString(TypeName.TnChangesDict); 
            var changes = await _keyValueDatabase.GetAsync(key, typeof(ChangesDict));
            var changesDict = changes == null ?  new ChangesDict() : ChangesDict.Parser.ParseFrom(changes);
            return new WorldState(changesDict);
        }
    }
}