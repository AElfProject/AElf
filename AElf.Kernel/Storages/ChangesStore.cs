using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly Dictionary<Hash, Change> _dictionary =
            new Dictionary<Hash, Change>();

        private readonly KeyValueDatabase _keyValueDatabase;

        public ChangesStore(KeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash pathHash, Change change)
        {
            await _keyValueDatabase.SetAsync(pathHash, change);
        }

        public async Task<Change> GetAsync(Hash pathHash)
        {
            return (Change) await _keyValueDatabase.GetAsync(pathHash, typeof(Change));
        }

        public Task<List<Change>> GetChangesAsync()
        {
            return Task.FromResult(_dictionary.Values.ToList());
        }

        public Task<List<Hash>> GetChangedPathHashesAsync()
        {
            return Task.FromResult(_dictionary.Keys.ToList());
        }

        public Task<Dictionary<Hash, Change>> GetChangesDictionary()
        {
            return Task.FromResult(_dictionary);
        }
    }
}