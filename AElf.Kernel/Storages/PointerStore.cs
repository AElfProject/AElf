using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class PointerStore : IPointerStore
    {
        private readonly KeyValueDatabase _keyValueDatabase;

        public PointerStore(KeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task UpdateAsync(Hash pathHash, Hash pointerHash)
        {
            await _keyValueDatabase.SetAsync(pathHash, pointerHash);
        }

        public async Task<Hash> GetAsync(Hash pathHash)
        {
            return (Hash) await _keyValueDatabase.GetAsync(pathHash, typeof(Hash));
        }
    }
}