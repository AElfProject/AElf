using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class ChainContextFactory : IChainContextFactory
    {
        
        // chain context cache
        private readonly Dictionary<IHash<IChain>, IChainContext> _chainContexts = new Dictionary<IHash<IChain>, IChainContext>();

        private readonly IChainManager _chainManager;

        public ChainContextFactory(IChainManager chainManager)
        {
            _chainManager = chainManager;
        }


        public async Task<IChainContext> GetChainContext(IHash<IChain> chainId)
        {
            if(_chainContexts.TryGetValue(chainId, out var chainContext))
            {
                return chainContext;
            }
            var chain = await _chainManager.GetAsync(chainId);
            var context = new ChainContext();
            context.Initialize(chain);
            _chainContexts[chainId] = context;

            // TODO : maintain cache for chain contexts
            return context;
        }
    }
}