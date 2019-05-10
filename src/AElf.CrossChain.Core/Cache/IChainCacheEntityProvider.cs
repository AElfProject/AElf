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
        ChainCacheEntity GetBlockInfoCache(int remoteChainId);

        bool ContainsChain(int remoteChainId);
        int Size { get; }
        List<int> CachedChainIds { get; }
    }
    
    public class ChainCacheEntityProvider : IChainCacheEntityProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<int, ChainCacheEntity> _blockInfoCaches = new ConcurrentDictionary<int, ChainCacheEntity>();
        
        public bool ContainsChain(int remoteChainId)
        {
            return _blockInfoCaches.ContainsKey(remoteChainId);
        }

        public int Size => _blockInfoCaches.Count;
        public List<int> CachedChainIds => _blockInfoCaches.Keys.ToList();

        public void AddChainCacheEntity(int remoteChainId, ChainCacheEntity chainCacheEntity)
        {
            if (chainCacheEntity == null)
                throw new ArgumentNullException(nameof(chainCacheEntity)); 
            _blockInfoCaches.TryAdd(remoteChainId, chainCacheEntity);
        }

        public ChainCacheEntity GetBlockInfoCache(int remoteChainId)
        {
            return !_blockInfoCaches.TryGetValue(remoteChainId, out _) ? null : _blockInfoCaches[remoteChainId];
        }
    }
}