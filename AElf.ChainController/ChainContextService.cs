using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;

namespace AElf.ChainController
{
    public class ChainContextService : IChainContextService
    {
        private readonly IChainService _chainService;
        public ChainContextService(IChainService chainService)
        {
            _chainService = chainService;
        }

        public async Task<IChainContext> GetChainContextAsync(Hash chainId = null)
        {
            if (chainId == null)
            {
                chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
            }
            
            var blockchain = _chainService.GetBlockChain(chainId);
            IChainContext chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = await blockchain.GetCurrentBlockHashAsync()
            };
            if (chainContext.BlockHash != Hash.Genesis && chainContext.BlockHash != null)
            {
                chainContext.BlockHeight = ((BlockHeader)await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }
            return chainContext;
        }
    }
}