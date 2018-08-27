using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class ChainManagerBasic : IChainManagerBasic
    {
        private readonly IDataStore _dataStore;

        public ChainManagerBasic(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _dataStore.InsertAsync(chainId.OfType(HashType.GenesisHash), genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            return await _dataStore.GetAsync<Hash>(chainId.OfType(HashType.GenesisHash));
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            Console.WriteLine("update current block hash to " + blockHash.ToHex());
            var key = chainId.OfType(HashType.CurrentHash);
            await _dataStore.InsertAsync(key, blockHash);
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var key = chainId.OfType(HashType.CurrentHash);
            var hash = await _dataStore.GetAsync<Hash>(key);
            return hash;
        }
    }
}