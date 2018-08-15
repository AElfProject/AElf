using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel;

namespace AElf.ChainController
{
    public class ChainContextService : IChainContextService
    {
        private IChainService _chainService;
        public ChainContextService(IChainService chainService)
        {
            _chainService = chainService;
        }

        public async Task<IChainContext> GetChainContextAsync(Hash chainId)
        {
            var blockchain = _chainService.GetBlockChain(chainId);
            IChainContext chainContext = new ChainContext()
            {
                ChainId = chainId,
                BlockHash = await blockchain.GetCurrentBlockHashAsync()
            };
            if (chainContext.BlockHash != Hash.Genesis)
            {
                chainContext.BlockHeight = ((BlockHeader)await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }
            return chainContext;
        }
    }
}