using AElf.Common;

namespace AElf.Kernel.SmartContract.Application
{
    public class ChainContextWithTieredStateCache : IChainContext<TieredStateCache>
    {
        public ChainContextWithTieredStateCache(IChainContext chainContext, TieredStateCache stateCache) : this(
            chainContext.BlockHash, chainContext.BlockHeight, stateCache)
        {
        }

        public ChainContextWithTieredStateCache(Hash blockHash, long blockHeight, TieredStateCache stateCache)
        {
            BlockHeight = blockHeight;
            BlockHash = blockHash;
            StateCache = stateCache;
        }

        public long BlockHeight { get; set; }
        public Hash BlockHash { get; set; }

        IStateCache IChainContext.StateCache
        {
            get => StateCache;
            set => StateCache = value as TieredStateCache;
        }

        public TieredStateCache StateCache { get; set; }
    }
}