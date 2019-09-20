using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IChainCacheEntityFactory
    {
        IChainCacheEntity CreateChainCacheEntity(int chainId, long chainHeight);
    }
    
    public class ChainCacheEntityFactory : IChainCacheEntityFactory, ITransientDependency
    {
        public IChainCacheEntity CreateChainCacheEntity(int chainId, long chainHeight)
        {
            return new ChainCacheEntity(chainId, chainHeight);
        }
    }
}