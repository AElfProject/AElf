using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        public ChangesStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertChangeAsync(Hash pathHash, Change change)
        {
            await _keyValueDatabase.SetAsync(pathHash, change.Serialize());
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            var value = await _keyValueDatabase.GetAsync(pathHash, typeof(Change));
            return value == null ? null : Change.Parser.ParseFrom(value);
        }

        public async Task UpdatePointerAsync(Hash pathHash, Hash pointerHash)
        {
            var change = await GetChangeAsync(pathHash);
            change.UpdateHashAfter(pointerHash);
            await _keyValueDatabase.SetAsync(pathHash, change.Serialize());
        }

        public async Task<Hash> GetPointerAsync(Hash pathHash)
        {
            var changeByte = await _keyValueDatabase.GetAsync(pathHash, typeof(Change));
            var change = Change.Parser.ParseFrom(changeByte);
            return change.After;
        }
    }
}