using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainCacheEntityProvider
    {
        void AddChainCacheEntity(int remoteChainId, long initialTargetHeight);
        BlockCacheEntityProvider GetChainCacheEntity(int remoteChainId);

        int Size { get; }
        List<int> GetCachedChainIds();
    }
    
    public class CrossChainCacheEntityProvider : ICrossChainCacheEntityProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<int, BlockCacheEntityProvider> _chainCacheEntities =
            new ConcurrentDictionary<int, BlockCacheEntityProvider>();
        
        public int Size => _chainCacheEntities.Count;
        
        public List<int> GetCachedChainIds()
        {
            return _chainCacheEntities.Keys.ToList();
        }

        public void AddChainCacheEntity(int remoteChainId, long initialTargetHeight)
        {
//            if (blockCacheEntityProvider == null)
//                throw new ArgumentNullException(nameof(blockCacheEntityProvider)); 
            _chainCacheEntities.TryAdd(remoteChainId, new BlockCacheEntityProvider(initialTargetHeight));
        }

        public BlockCacheEntityProvider GetChainCacheEntity(int remoteChainId)
        {
            return !_chainCacheEntities.TryGetValue(remoteChainId, out var blockCacheEntityProvider) ? null : blockCacheEntityProvider;
        }
    }
}