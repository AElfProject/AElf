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

        public async Task InsertAsync(Hash key, Change change)
        {
            await _keyValueDatabase.SetAsync(key, change.ToByteArray());
        }

        public async Task<Change> GetAsync(Hash key)
        {
            var value = await _keyValueDatabase.GetAsync(key, typeof(Change));
            return value == null ? null : Change.Parser.ParseFrom(value);
        }
    }
}