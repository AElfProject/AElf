using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;

        public CrossChainTestBase()
        {
            CrossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
            GetRequiredService<IOptionsMonitor<CrossChainConfigOptions>>().CurrentValue
                .CrossChainDataValidationIgnored = false;
        }

        protected void CreateFakeCache(Dictionary<int, BlockCacheEntityProvider> cachingData)
        {
            foreach (var (key, value) in cachingData)
            {
                CrossChainCacheEntityProvider.AddChainCacheEntity(key, value.TargetChainHeight());
            }
        }

        protected void AddFakeCacheData(Dictionary<int, List<IBlockCacheEntity>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                CrossChainCacheEntityProvider.AddChainCacheEntity(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    BlockCacheEntityProducer.TryAddBlockCacheEntity(blockInfo);
                }
            }
        }
    }

    public class CrossChainWithChainTestBase : AElfIntegratedTest<CrossChainWithChainTestModule>
    {
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

        protected CrossChainWithChainTestBase()
        {
            _blockCacheEntityProducer = Application.ServiceProvider.GetRequiredService<IBlockCacheEntityProducer>();
            _crossChainCacheEntityProvider = Application.ServiceProvider.GetRequiredService<ICrossChainCacheEntityProvider>();
        }
        
        protected void AddFakeCacheData(Dictionary<int, List<IBlockCacheEntity>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                _crossChainCacheEntityProvider.AddChainCacheEntity(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    _blockCacheEntityProducer.TryAddBlockCacheEntity(blockInfo);
                }
            }
        }
    }
}