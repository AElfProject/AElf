using System;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.Change;

        public ChangesStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertChangeAsync(Hash pathHash, Change change)
        {
            var key = pathHash.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, change.Serialize());
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            var key = pathHash.GetKeyString(TypeIndex);
            var value = await _keyValueDatabase.GetAsync(key, typeof(Change));
            return value == null ? null : Change.Parser.ParseFrom(value);
        }

        public async Task UpdatePointerAsync(Hash pathHash, Hash pointerHash)
        {
            var key = pathHash.GetKeyString(TypeIndex);
            var change = await GetChangeAsync(pathHash);
            change.UpdateHashAfter(pointerHash);
            await _keyValueDatabase.SetAsync(key, change.Serialize());
        }

        public async Task<Hash> GetPointerAsync(Hash pathHash)
        {
            var key = pathHash.GetKeyString(TypeIndex);
            var changeByte = await _keyValueDatabase.GetAsync(key, typeof(Change));
            var change = changeByte == null ? null : Change.Parser.ParseFrom(changeByte);
            return change?.After;
        }
    }
}