using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Common;

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
//            if (hash.HashType == HashType.General)
//            {
//                throw new InvalidOperationException("Please use other managers to get data via general hash.");
//            }

            return await _dataStore.GetAsync<Hash>(hash);
        }

        public async Task SetHash(Hash hash, Hash another)
        {
//            if (hash.HashType == HashType.General)
//            {
//                throw new InvalidOperationException("Please use other managers to set data via general hash.");
//            }
//            
            await _dataStore.InsertAsync(hash, another);
        }
    }
}