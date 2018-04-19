using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly KeyValueDatabase _keyValueDatabase;
        private readonly KeyValueDatabase _keyValueDatabaseForPaths;

        private static readonly Hash HashToGetPaths = new Hash("paths".CalculateHash());

        public ChangesStore(KeyValueDatabase keyValueDatabase, KeyValueDatabase keyValueDatabaseForPaths)
        {
            _keyValueDatabase = keyValueDatabase;
            _keyValueDatabaseForPaths = keyValueDatabaseForPaths;
            _keyValueDatabaseForPaths.SetAsync(HashToGetPaths, new List<Hash>());
        }

        public async Task InsertAsync(Hash pathHash, Change change)
        {
            await _keyValueDatabase.SetAsync(pathHash, change);
            
            var paths = (List<Hash>)await _keyValueDatabaseForPaths.GetAsync(HashToGetPaths, typeof(List<Hash>));
            paths.Add(pathHash);
            await _keyValueDatabaseForPaths.SetAsync(HashToGetPaths, paths);
        }

        public async Task<Change> GetAsync(Hash pathHash)
        {
            return (Change) await _keyValueDatabase.GetAsync(pathHash, typeof(Change));
        }

        public async Task<List<Change>> GetChangesAsync()
        {
            var changes = new List<Change>();
            var paths = (List<Hash>)await _keyValueDatabaseForPaths.GetAsync(HashToGetPaths, typeof(List<Hash>));
            foreach (var path in paths)
            {
                var change = (Change) await _keyValueDatabase.GetAsync(path, typeof(Change));
                changes.Add(change);
            }
            return changes;
        }

        public async Task<List<Hash>> GetChangedPathHashesAsync()
        {
            return (List<Hash>) await _keyValueDatabaseForPaths.GetAsync(HashToGetPaths, typeof(List<Hash>));
        }

        public async Task<Dictionary<Hash, Change>> GetChangesDictionary()
        {
            var dict = new Dictionary<Hash, Change>();
            var paths = (List<Hash>)await _keyValueDatabaseForPaths.GetAsync(HashToGetPaths, typeof(List<Hash>));
            foreach (var path in paths)
            {
                var change = (Change) await _keyValueDatabase.GetAsync(path, typeof(Change));
                dict.Add(path, change);
            }
            return dict;
        }
    }
}