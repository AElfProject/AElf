using AElf.Common;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class ChainContextWithTieredStateCache : IChainContext<TieredStateCache>
    {
        public ChainContextWithTieredStateCache(IChainContext chainContext, TieredStateCache stateCache)
        {
            ChainId = chainContext.ChainId;
            BlockHeight = chainContext.BlockHeight;
            BlockHash = chainContext.BlockHash;
            StateCache = stateCache;
        }

        public int ChainId { get; set; }
        public ulong BlockHeight { get; set; }
        public Hash BlockHash { get; set; }

        IStateCache IChainContext.StateCache
        {
            get => StateCache;
            set => StateCache = value as TieredStateCache;
        }

        public TieredStateCache StateCache { get; set; }
    }
}