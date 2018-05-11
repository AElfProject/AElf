using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task SetDataAsync(Hash pointerHash, byte[] data)
        {
            await _keyValueDatabase.SetAsync(pointerHash.Value.ToBase64(), data);
        }

        public async Task<byte[]> GetDataAsync(Hash pointerHash)
        {
            return (byte[]) await _keyValueDatabase.GetAsync(pointerHash.Value.ToBase64(), typeof(byte[]));
        }
    }
}