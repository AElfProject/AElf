namespace AElf.CrossChain
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        ulong GetChainHeightNeededForCache(int chainId);
    }
}