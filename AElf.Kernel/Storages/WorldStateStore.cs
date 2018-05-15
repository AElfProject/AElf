using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Extensions;
using Google.Protobuf;

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
            await _keyValueDatabase.SetAsync(wsKey.Value.ToBase64(), changes.Serialize());
        }

        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            Hash wsKey = chainId.CalculateHashWith(blockHash);
            var changesDict = ChangesDict.Parser.ParseFrom(await _keyValueDatabase.GetAsync(wsKey.Value.ToBase64() , typeof(ChangesDict)));
            return new WorldState(changesDict);
        }
    }
}