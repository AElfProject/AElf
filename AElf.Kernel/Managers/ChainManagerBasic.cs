using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class ChainManagerBasic : IChainManagerBasic
    {
        private readonly IGenesisHashStore _genesisHashStore;
        private readonly ICurrentHashStore _currentHashStore;

        public ChainManagerBasic(IGenesisHashStore genesisHashStore, ICurrentHashStore currentHashStore)
        {
            _genesisHashStore = genesisHashStore;
            _currentHashStore = currentHashStore;
        }

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _genesisHashStore.InsertAsync(chainId, genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            var hash = await _genesisHashStore.GetAsync(chainId);
            return hash;
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            await _currentHashStore.InsertOrUpdateAsync(chainId, blockHash);
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var hash = await _currentHashStore.GetAsync(chainId);
            return hash;
        }
    }
}