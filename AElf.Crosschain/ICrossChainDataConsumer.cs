using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataConsumer
    {
        BlockInfoCache BlockInfoCache { get; set; }
        
        int ChainId { get; }
        
        IBlockInfo TryTake(ulong height, bool isCacheSizeLimited);
    }
}