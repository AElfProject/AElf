using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

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
            await _keyValueDatabase.SetAsync(pointerHash.ToHex(), data);
        }

        public async Task<byte[]> GetDataAsync(Hash pointerHash)
        {
            if (pointerHash == null)
            {
                return null;
            }
            return await _keyValueDatabase.GetAsync(pointerHash.ToHex(), typeof(byte[]));
        }
    }
}