using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainCacheEntityProvider
    {
        void AddChainCacheEntity(int remoteChainId, long initialTargetHeight);
        IChainCacheEntity GetChainCacheEntity(int remoteChainId);
        int Size { get; }
        List<int> GetCachedChainIds();
    }
    
    public class CrossChainCacheEntityProvider : ICrossChainCacheEntityProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<int, IChainCacheEntity> _chainCacheEntities =
            new ConcurrentDictionary<int, IChainCacheEntity>();
        
        public int Size => _chainCacheEntities.Count;
        
        public List<int> GetCachedChainIds()
        {
            return _chainCacheEntities.Keys.ToList();
        }

        public void AddChainCacheEntity(int remoteChainId, long initialTargetHeight)
        {
            var chainCacheEntity = new ChainCacheEntity(remoteChainId, initialTargetHeight);
            _chainCacheEntities.TryAdd(remoteChainId, chainCacheEntity);
        }

        public IChainCacheEntity GetChainCacheEntity(int remoteChainId)
        {
            return !_chainCacheEntities.TryGetValue(remoteChainId, out var chainCacheEntity)
                ? null
                : chainCacheEntity;
        }
    }
}