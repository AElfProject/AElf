namespace AElf.CrossChain
{
    public interface ICrossChainDataConsumer
    {
//        BlockInfoCache BlockInfoCache { get; set; }
//        
//        int ChainId { get; }
        
        IBlockInfo TryTake(int crossChainId, ulong height, bool isCacheSizeLimited);
    }
}