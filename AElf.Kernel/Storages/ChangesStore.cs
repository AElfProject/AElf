using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        /// <summary>
        /// Use another KeyValueDatabase to store all the changed paths of current world state.
        /// Use HashToGetPathsCount to get the count of changed paths.
        /// </summary>
        private readonly IKeyValueDatabase _keyValueDatabaseForPaths;

        private static readonly Hash HashToGetPathsCount = Hash.Zero;

        public ChangesStore(IKeyValueDatabase keyValueDatabase, IKeyValueDatabase keyValueDatabaseForPaths)
        {
            _keyValueDatabase = keyValueDatabase;
            _keyValueDatabaseForPaths = keyValueDatabaseForPaths;
            _keyValueDatabaseForPaths.SetAsync(HashToGetPathsCount, 0);
        }

        public async Task InsertAsync(Hash pathHash, Change change)
        {
            await _keyValueDatabase.SetAsync(pathHash, change);
            
            var count = (int)await _keyValueDatabaseForPaths.GetAsync(HashToGetPathsCount, typeof(int));
            count++;
            await _keyValueDatabaseForPaths.SetAsync(HashToGetPathsCount, count);
            await _keyValueDatabaseForPaths.SetAsync(IntToHash(count), pathHash);
        }

        public async Task<Change> GetAsync(Hash pathHash)
        {
            return (Change) await _keyValueDatabase.GetAsync(pathHash, typeof(Change));
        }

        /// <summary>
        /// Use all the changed paths to get all the relative Changes.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Change>> GetChangesAsync()
        {
            var changes = new List<Change>();
            var count = (int)await _keyValueDatabaseForPaths.GetAsync(HashToGetPathsCount, typeof(int));
            for (var i = 1; i < count + 1; i++)
            {
                var changedPathHash = (Hash)await _keyValueDatabaseForPaths.GetAsync(IntToHash(i), typeof(Hash));
                var change = (Change) await _keyValueDatabase.GetAsync(changedPathHash, typeof(Change));
                changes.Add(change);
            }
 
            return changes;
        }

        /// <summary>
        /// Simply return all the changed paths.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Hash>> GetChangedPathHashesAsync()
        {
            var count = (int)await _keyValueDatabaseForPaths.GetAsync(HashToGetPathsCount, typeof(int));
            var paths = new List<Hash>();
            for (var i = 1; i < count + 1; i++)
            {
                var changedPathHash = (Hash)await _keyValueDatabaseForPaths.GetAsync(IntToHash(i), typeof(Hash));
                paths.Add(changedPathHash);
            }

            return paths;
        }

        public async Task<Dictionary<Hash, Change>> GetChangesDictionary()
        {
            var dict = new Dictionary<Hash, Change>();
            var count = (int)await _keyValueDatabaseForPaths.GetAsync(HashToGetPathsCount, typeof(int));
            for (var i = 1; i < count + 1; i++)
            {
                var changedPathHash = (Hash)await _keyValueDatabaseForPaths.GetAsync(IntToHash(i), typeof(Hash));
                var change = (Change) await _keyValueDatabase.GetAsync(changedPathHash, typeof(Change));
                dict[changedPathHash] = change;
            }

            return dict;
        }

        private Hash IntToHash(int number)
        {
            var newHashValue = new byte[32];
            for (var i = 0; i < 4; i++)
            {
                newHashValue[i] = (byte) ((number >> (8 * i)) & 0xff);
            }

            return new Hash(newHashValue);
        }
    }
}