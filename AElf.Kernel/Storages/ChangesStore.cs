using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly Dictionary<Hash, Change> _dictionary =
            new Dictionary<Hash, Change>();

        public Task InsertAsync(Hash pathHash, Change change)
        {
            _dictionary[pathHash] = change;
            return Task.CompletedTask;
        }

        public Task<Change> GetAsync(Hash pathHash)
        {
            return _dictionary.TryGetValue(pathHash, out var change) ? Task.FromResult(change) : null;
        }

        public Task<List<Change>> GetChangesAsync()
        {
            return Task.FromResult(_dictionary.Values.ToList());
        }

        public Task<List<Hash>> GetChangedPathsAsync()
        {
            return Task.FromResult(_dictionary.Keys.ToList());
        }

        public object Clone()
        {
            var changesStore = new ChangesStore();
            foreach (var key in _dictionary.Keys)
            {
                changesStore.InsertAsync(key, _dictionary[key]);
            }
            return changesStore;
        }
    }
}