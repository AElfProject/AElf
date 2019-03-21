namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataConsumer
    {
        IBlockInfo TryTake(int crossChainId, long height, bool isCacheSizeLimited);
        int GetCachedChainCount();
        void TryRegisterNewChainCache(int remoteChainId, long chainHeight = ChainConsts.GenesisBlockHeight);
        bool CheckAlreadyCachedChain(int remoteChainId);
    }
}