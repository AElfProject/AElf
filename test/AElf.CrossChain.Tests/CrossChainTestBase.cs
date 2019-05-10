using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected IChainCacheEntityProvider ChainCacheEntityProvider;
        protected ICrossChainDataProducer CrossChainDataProducer;
        protected ICrossChainDataConsumer CrossChainDataConsumer;

        public CrossChainTestBase()
        {
            ChainCacheEntityProvider = GetRequiredService<IChainCacheEntityProvider>();
            CrossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
        }

        protected void CreateFakeCache(Dictionary<int, ChainCacheEntity> cachingData)
        {
            foreach (var (key, value) in cachingData)
            {
                ChainCacheEntityProvider.AddChainCacheEntity(key, value);
            }
        }

        protected void AddFakeCacheData(Dictionary<int, List<BlockCacheEntity>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                ChainCacheEntityProvider.AddChainCacheEntity(crossChainId,
                    new ChainCacheEntity(blockInfos.First().Height));
                foreach (var blockInfo in blockInfos)
                {
                    CrossChainDataProducer.AddCacheEntity(blockInfo);
                }
            }
        }
    }

    public class CrossChainWithChainTestBase : AElfIntegratedTest<CrossChainWithChainTestModule>
    {
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        protected CrossChainWithChainTestBase()
        {
            _crossChainDataProducer = Application.ServiceProvider.GetRequiredService<ICrossChainDataProducer>();
            _chainCacheEntityProvider = Application.ServiceProvider.GetRequiredService<IChainCacheEntityProvider>();
        }
        
        protected void AddFakeCacheData(Dictionary<int, List<BlockCacheEntity>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                _chainCacheEntityProvider.AddChainCacheEntity(crossChainId,
                    new ChainCacheEntity(blockInfos.First().Height));
                foreach (var blockInfo in blockInfos)
                {
                    _crossChainDataProducer.AddCacheEntity(blockInfo);
                }
            }
        }
    }
}