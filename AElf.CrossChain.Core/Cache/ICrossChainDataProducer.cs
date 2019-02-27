namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        ulong GetChainHeightNeededForCache(int chainId);
    }
}