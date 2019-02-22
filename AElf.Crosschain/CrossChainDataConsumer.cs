using AElf.Kernel;

namespace AElf.Crosschain
{
    public class CrossChainDataConsumer : ICrossChainDataConsumer
    {
        public BlockInfoCache BlockInfoCache { get; set; }
        public int ChainId { get; set; }

        public IBlockInfo TryTake(ulong height, bool isCacheSizeLimited)
        {
            return BlockInfoCache.TryTake(height, out var blockInfo, isCacheSizeLimited) ? blockInfo : null;
        }
    }
}