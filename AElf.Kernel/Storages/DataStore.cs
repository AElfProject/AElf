using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task SetDataAsync(Hash pointerHash, Data data)
        {
            await _keyValueDatabase.SetAsync(pointerHash.Value.ToBase64(), data);
        }

        public async Task<Data> GetDataAsync(Hash pointerHash)
        {
            if (pointerHash == null)
            {
                return null;
            }

            var data = await _keyValueDatabase.GetAsync(pointerHash.Value.ToBase64(), typeof(Data));
            return data == null ? null : Data.Parser.ParseFrom(data);
        }
    }
}