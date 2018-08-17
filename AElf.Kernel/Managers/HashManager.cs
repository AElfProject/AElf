using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class HashManager : IHashManager
    {
        private readonly IDataStore _dataStore;

        public HashManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<Hash> GetHash(Hash hash)
        {
            var key = GetKey(hash);
            return await _dataStore.GetAsync<Hash>(key);
        }

        public async Task SetHash(Hash hash, Hash another)
        {
            var key = GetKey(hash);
            await _dataStore.InsertAsync(key, another);
        }

        private Hash GetKey(Hash hash)
        {
            var key = hash;
            var type = hash.HashType;
            switch (type)
            {
                case HashType.ResourcePath:
                    key = hash.CalculateHashWith(type.ToString());
                    break;
            }

            return key;
        }
    }
}