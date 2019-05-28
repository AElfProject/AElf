using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Application
{
    public interface ICrossChainCacheEntityService
    {
        Task RegisterNewChainsAsync(Hash blockHash, long blockHeight);
        List<int> GetCachedChainIds();
        long GetTargetHeightForChainCacheEntity(int chainId);
    }

    internal class CrossChainCacheEntityService : ICrossChainCacheEntityService, ITransientDependency
    {
        private readonly IReaderFactory _readerFactory;
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        public CrossChainCacheEntityService(IReaderFactory readerFactory, ICrossChainCacheEntityProvider crossChainCacheEntityProvider)
        {
            _readerFactory = readerFactory;
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
        }

        public async Task RegisterNewChainsAsync(Hash blockHash, long blockHeight)
        {
            var dict = await _readerFactory.Create(blockHash, blockHeight).GetAllChainsIdAndHeight
                .CallAsync(new Empty());

            foreach (var chainIdHeight in dict.IdHeightDict)
            {
                _crossChainCacheEntityProvider.AddChainCacheEntity(chainIdHeight.Key, chainIdHeight.Value + 1);
            }
        }

        public List<int> GetCachedChainIds()
        {
            return _crossChainCacheEntityProvider.CachedChainIds;
        }

        public long GetTargetHeightForChainCacheEntity(int chainId)
        {
            return _crossChainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
        }
    }
}