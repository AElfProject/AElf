using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel
{
    public class ChainContextFactory : IChainContextFactory
    {
        private readonly Dictionary<IHash<IChain>, IChainContext> _chainContexts =
            new Dictionary<IHash<IChain>, IChainContext>();
            
        public IChainContext GetChainContext(IHash<IChain> chainHash)
        {
            var res = _chainContexts.TryGetValue(chainHash, out var chainContext);
            if (res) return chainContext;
            throw new KeyNotFoundException();
        }
    }
}