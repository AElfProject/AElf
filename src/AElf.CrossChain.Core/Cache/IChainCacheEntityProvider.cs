using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface IChainCacheEntityProvider
    {
        void AddChainCacheEntity(int remoteChainId, ChainCacheEntity chainCacheEntity);
        ChainCacheEntity GetChainCacheEntity(int remoteChainId);

        bool ContainsChain(int remoteChainId);
        int Size { get; }
        List<int> CachedChainIds { get; }
    }
    
    public class ChainCacheEntityProvider : IChainCacheEntityProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<int, ChainCacheEntity> _chainCacheEntities = new ConcurrentDictionary<int, ChainCacheEntity>();
        
        public bool ContainsChain(int remoteChainId)
        {
            return _chainCacheEntities.ContainsKey(remoteChainId);
        }

        public int Size => _chainCacheEntities.Count;
        public List<int> CachedChainIds => _chainCacheEntities.Keys.ToList();

        public void AddChainCacheEntity(int remoteChainId, ChainCacheEntity chainCacheEntity)
        {
            if (chainCacheEntity == null)
                throw new ArgumentNullException(nameof(chainCacheEntity)); 
            _chainCacheEntities.TryAdd(remoteChainId, chainCacheEntity);
        }

        public ChainCacheEntity GetChainCacheEntity(int remoteChainId)
        {
            return !_chainCacheEntities.TryGetValue(remoteChainId, out var chainCacheEntity) ? null : chainCacheEntity;
        }
    }
}