using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public ChangesStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
        
        public async Task InsertAsync(Hash path, Change change)
        {
            await _keyValueDatabase.SetAsync(path, change);
        }

        public async Task<Change> GetAsync(Hash path)
        {
            return (Change) await _keyValueDatabase.GetAsync(path, typeof(Change));
        }

        public object Clone()
        {
            var kvDatabase = (IKeyValueDatabase)_keyValueDatabase.Clone();
            return new ChangesStore(kvDatabase);
        }
    }
}