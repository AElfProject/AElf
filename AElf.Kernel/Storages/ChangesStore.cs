using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Storages
{
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        public ChangesStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash key, Change change)
        {
            await _keyValueDatabase.SetAsync(key, change);
        }

        public async Task<Change> GetAsync(Hash key)
        {
            return (Change) await _keyValueDatabase.GetAsync(key, typeof(Change));
        }
    }
}