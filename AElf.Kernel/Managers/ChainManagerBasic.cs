using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class ChainManagerBasic : IChainManagerBasic
    {
        private readonly IGenesisHashStore _genesisHashStore;
        private readonly ICurrentHashStore _currentHashStore;
        private readonly ICanonicalHashStore _canonicalHashStore;

        // TODO: Replace ChainManager class with this class
        public ChainManagerBasic(IGenesisHashStore genesisHashStore, ICurrentHashStore currentHashStore, ICanonicalHashStore canonicalHashStore)
        {
            _genesisHashStore = genesisHashStore;
            _currentHashStore = currentHashStore;
            _canonicalHashStore = canonicalHashStore;
        }

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _genesisHashStore.InsertAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            var hash = await _genesisHashStore.GetAsync(chainId);
            return hash;
        }

        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var hash = await _currentHashStore.GetAsync(chainId);
            return hash;
        }

    }
}