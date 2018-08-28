using System;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class CurrentHashStore : ICurrentHashStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.CurrentHash;

        public CurrentHashStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertOrUpdateAsync(Hash chainId, Hash currentHash)
        {
            var key = chainId.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, currentHash.ToByteArray());
        }

        public async Task<Hash> GetAsync(Hash chainId)
        {
            var key = chainId.GetKeyString(TypeIndex);
            var hash = await _keyValueDatabase.GetAsync(key);
            if (hash == null)
            {
                return null;   
            }
            return Hash.Parser.ParseFrom(hash);
        }
    }
}