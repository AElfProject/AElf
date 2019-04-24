using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Cache;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected IMultiChainBlockInfoCacheProvider MultiChainBlockInfoCacheProvider;
        protected ICrossChainDataProducer CrossChainDataProducer;
        protected ICrossChainDataConsumer CrossChainDataConsumer;
        protected ICrossChainMemoryCacheService CrossChainMemoryCacheService;

        public CrossChainTestBase()
        {
            MultiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
            CrossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            CrossChainMemoryCacheService = GetRequiredService<ICrossChainMemoryCacheService>();
        }

        protected void CreateFakeCache(Dictionary<int, BlockInfoCache> cachingData)
        {
            foreach (var (key, value) in cachingData)
            {
                MultiChainBlockInfoCacheProvider.AddBlockInfoCache(key, value);
            }
        }

        protected void AddFakeCacheData(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                CrossChainMemoryCacheService.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    CrossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }

    public class CrossChainWithChainTestBase : AElfIntegratedTest<CrossChainWithChainTestModule>
    {
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        private readonly ICrossChainMemoryCacheService _crossChainMemoryCacheService;

        protected CrossChainWithChainTestBase()
        {
            _crossChainDataProducer = Application.ServiceProvider.GetRequiredService<ICrossChainDataProducer>();
            _crossChainMemoryCacheService = Application.ServiceProvider.GetRequiredService<ICrossChainMemoryCacheService>();
        }
        
        protected void AddFakeCacheData(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                _crossChainMemoryCacheService.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    _crossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }
}