namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataConsumer
    {
        IBlockInfo TryTake(int crossChainId, long height, bool isCacheSizeLimited);
        int GetCachedChainCount();
        void RegisterNewChainCache(int remoteChainId, long chainHeight = CrossChainConsts.GenesisBlockHeight);
        bool CheckAlreadyCachedChain(int remoteChainId);
    }
}