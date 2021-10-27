using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Cache.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface ICrossChainCacheEntityService
    {
        void RegisterNewChain(int chainId, long height);
        List<int> GetCachedChainIds();
        long GetTargetHeightForChainCacheEntity(int chainId);

        Task UpdateCrossChainCacheAsync(Hash blockHash, long blockHeight,
            ChainIdAndHeightDict chainIdAndHeightDict);
    }

    internal class CrossChainCacheEntityService : ICrossChainCacheEntityService, ITransientDependency
    {
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public ILogger<CrossChainCacheEntityService> Logger { get; set; }

        public CrossChainCacheEntityService(ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public void RegisterNewChain(int chainId, long height)
        {
            if (_crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out _))
                return;
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

        private void ClearOutOfDateCrossChainCache(int chainId, long height)
        {
            if (_crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var chainCacheEntity))
            {
                chainCacheEntity.ClearOutOfDateCacheByHeight(height);
            }
        }

        public Task UpdateCrossChainCacheAsync(Hash blockHash, long blockHeight,
            ChainIdAndHeightDict chainIdAndHeightDict)
        {
            foreach (var chainIdHeight in chainIdAndHeightDict.IdHeightDict)
            {
                // register new chain
                RegisterNewChain(chainIdHeight.Key, chainIdHeight.Value);

                // clear cross chain cache
                ClearOutOfDateCrossChainCache(chainIdHeight.Key, chainIdHeight.Value);
                Logger.LogDebug(
                    $"Clear chain {ChainHelper.ConvertChainIdToBase58(chainIdHeight.Key)} cache by height {chainIdHeight.Value}");
            }

            return Task.CompletedTask;
        }
    }
}