namespace AElf.CrossChain
{
    public interface ICrossChainDataConsumer
    {
        IBlockInfo TryTake(int crossChainId, ulong height, bool isCacheSizeLimited);
        int GetCachedChainCount();
        void RegisterNewChainCache(int chainId, ulong chainHeight);
    }
}