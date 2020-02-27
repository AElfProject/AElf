using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface ICrossChainCacheEntityService
    {
        void RegisterNewChain(int chainId, long height);
        List<int> GetCachedChainIds();
        long GetTargetHeightForChainCacheEntity(int chainId);
        void ClearOutOfDateCrossChainCache(int chainId, long height);
    }

    internal class CrossChainCacheEntityService : ICrossChainCacheEntityService, ITransientDependency
    {
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public CrossChainCacheEntityService(ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public void RegisterNewChain(int chainId, long height)
        {
            _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, height + 1);
        }

        public List<int> GetCachedChainIds()
        {
            return _crossChainCacheEntityProvider.GetCachedChainIds();
        }

        public long GetTargetHeightForChainCacheEntity(int chainId)
        {
            if (!_crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var chainCacheEntity))
            {
                throw new InvalidOperationException($"ChainCacheEntity of {chainId} not found");
            }

            return chainCacheEntity.TargetChainHeight();
        }

        public void ClearOutOfDateCrossChainCache(int chainId, long height)
        {
            if (_crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var chainCacheEntity))
            {
                chainCacheEntity.ClearOutOfDateCacheByHeight(height);
            }
        }
    }
}