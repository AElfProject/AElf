using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class PointerManager : IPointerManager
    {
        private readonly IDataStore _dataStore;

        public PointerManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task AddPointerAsync(Hash pointer, Hash value)
        {
            await _dataStore.InsertAsync(pointer, value);
        }

        public async Task UpdatePointerAsync(Hash pointer, Hash value)
        {
            await _dataStore.InsertAsync(pointer, value);
        }

        public async Task<Hash> GetPointerAsync(Hash pointer)
        {
            return await _dataStore.GetAsync<Hash>(pointer);
        }

        public async Task RemovePointer(Hash pointer)
        {
            await _dataStore.RemoveAsync<Hash>(pointer);
        }
    }
}