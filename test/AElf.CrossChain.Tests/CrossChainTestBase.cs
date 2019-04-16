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
        protected ICrossChainMemCacheService CrossChainMemCacheService;

        public CrossChainTestBase()
        {
            MultiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
            CrossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            CrossChainMemCacheService = GetRequiredService<ICrossChainMemCacheService>();
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
                CrossChainMemCacheService.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
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
        private readonly ICrossChainMemCacheService _crossChainMemCacheService;

        protected CrossChainWithChainTestBase()
        {
            _crossChainDataProducer = Application.ServiceProvider.GetRequiredService<ICrossChainDataProducer>();
            _crossChainMemCacheService = Application.ServiceProvider.GetRequiredService<ICrossChainMemCacheService>();
        }
        
        protected void AddFakeCacheData(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                _crossChainMemCacheService.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    _crossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }
}