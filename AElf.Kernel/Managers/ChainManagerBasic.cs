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
            await _dataStore.InsertAsync(chainId.SetHashType(HashType.GenesisHash), genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            var hash = await _dataStore.GetAsync(chainId.SetHashType(HashType.GenesisHash));
            return hash;
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            await _dataStore.InsertAsync(chainId.SetHashType(HashType.CurrentHash), blockHash);
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var hash = await _dataStore.GetAsync(chainId.SetHashType(HashType.CurrentHash));
            return hash;
        }
    }
}