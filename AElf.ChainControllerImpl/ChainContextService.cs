using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel;

namespace AElf.ChainController
{
    public class ChainContextService : IChainContextService
    {
        private IChainManager _chainManager;
        public ChainContextService(IChainManager chainManager)
        {
            _chainManager = chainManager;
        }

        public async Task<IChainContext> GetChainContextAsync(Hash chainId)
        {
            IChainContext chainContext = new ChainContext()
            {
                ChainId = chainId,
                BlockHeight = await _chainManager.GetChainCurrentHeight(chainId),
                BlockHash = await _chainManager.GetChainLastBlockHash(chainId)
            };
            return chainContext;
        }
    }
}