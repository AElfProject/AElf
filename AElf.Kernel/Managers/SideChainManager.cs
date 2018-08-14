using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class SideChainManager : ISideChainManager
    {
        private readonly ISideChainStore _sideChainStore;

        public SideChainManager(ISideChainStore sideChainStore)
        {
            _sideChainStore = sideChainStore;
        }

        public async Task<SideChain> GetSideChainAsync(Hash chainId)
        {
            return await _sideChainStore.GetAsync(chainId);
        }

        public async Task SetSideChainAsync(SideChain sideChain)
        {
            await _sideChainStore.InsertAsync(sideChain);
        }
    }
}