namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        long GetChainHeightNeededForCache(int chainId);
    }
}