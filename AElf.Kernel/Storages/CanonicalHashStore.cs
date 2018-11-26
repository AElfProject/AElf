using System;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;


namespace AElf.Kernel.Storages
{
    public class CanonicalHashStore : ICanonicalHashStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.CanonicalBlockHash;

        public CanonicalHashStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task<Hash> InsertOrUpdateAsync(Hash heightHash, Hash blockHash)
        {
            var key = heightHash.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, blockHash.ToByteArray());
            return blockHash;
        }

        public async Task<Hash> GetAsync(Hash heightHash)
        {
            var key = heightHash.GetKeyString(TypeIndex);
            var bytes = await _keyValueDatabase.GetAsync(key);
            if (bytes == null)
            {
                return null;
            }
            return Hash.Parser.ParseFrom(bytes);
        }

        public async Task RemoveAsync(Hash heightHash)
        {
            var key = heightHash.GetKeyString(TypeIndex);
            await _keyValueDatabase.RemoveAsync(key);
        }
    }
}