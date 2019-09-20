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
        private readonly IChainCacheEntityFactory _chainCacheEntityFactory;
        
        private readonly ConcurrentDictionary<int, IChainCacheEntity> _chainCacheEntities =
            new ConcurrentDictionary<int, IChainCacheEntity>();

        public CrossChainCacheEntityProvider(IChainCacheEntityFactory chainCacheEntityFactory)
        {
            _chainCacheEntityFactory = chainCacheEntityFactory;
        }

        public int Size => _chainCacheEntities.Count;
        
        public List<int> GetCachedChainIds()
        {
            return _chainCacheEntities.Keys.ToList();
        }

        public void AddChainCacheEntity(int remoteChainId, long initialTargetHeight)
        {
            var chainCacheEntity = _chainCacheEntityFactory.CreateChainCacheEntity(remoteChainId, initialTargetHeight);
            _chainCacheEntities.TryAdd(remoteChainId, chainCacheEntity);
        }

        public IChainCacheEntity GetChainCacheEntity(int remoteChainId)
        {
            return !_chainCacheEntities.TryGetValue(remoteChainId, out var blockCacheEntityProvider)
                ? null
                : blockCacheEntityProvider;
        }
    }
}