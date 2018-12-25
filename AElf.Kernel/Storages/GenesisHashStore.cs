using System;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class GenesisHashStore : IGenesisHashStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.GenesisHash;

        public GenesisHashStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash chainId, Hash genesisHash)
        {
            var key = chainId.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, genesisHash.ToByteArray());
        }

        public async Task<Hash> GetAsync(Hash chainId)
        {
            var key = chainId.GetKeyString(TypeIndex);
            var hash = await _keyValueDatabase.GetAsync(key);
            return Hash.Parser.ParseFrom(hash);
        }
    }
}