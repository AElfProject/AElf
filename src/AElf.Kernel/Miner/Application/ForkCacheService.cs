using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public class ForkCacheService : IForkCacheService
    {
        private readonly List<IForkCacheHandler> _forkCacheHandlers;


        public ForkCacheService(IServiceContainer<IForkCacheHandler> forkCacheHandlers)
        {
            _forkCacheHandlers = forkCacheHandlers.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }
        
        public void RemoveByBlockHash(List<Hash> blockHashes)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                forkCacheHandler.RemoveForkCache(blockHashes);
            }
        }

        public void SetIrreversible(List<Hash> blockHashes)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                forkCacheHandler.SetIrreversedCache(blockHashes);
            }
        }

        public void SetIrreversible(Hash blockHash)
        {
            foreach (var forkCacheHandler in _forkCacheHandlers)
            {
                forkCacheHandler.SetIrreversedCache(blockHash);
            }
        }
    }
}