using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Cache.Infrastructure;
using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;
        protected CrossChainConfigOptions _configOptions;
        protected ChainOptions _chainOptions;

        public CrossChainTestBase()
        {
            CrossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
            _configOptions = GetRequiredService<IOptionsMonitor<CrossChainConfigOptions>>().CurrentValue;
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _configOptions.CrossChainDataValidationIgnored = false;
        }

        protected void CreateFakeCache(Dictionary<int, ChainCacheEntity> cachingData)
        {
            foreach (var (key, value) in cachingData)
            {
                CrossChainCacheEntityProvider.AddChainCacheEntity(key, value.TargetChainHeight());
            }
        }

        protected void AddFakeCacheData(Dictionary<int, List<ICrossChainBlockEntity>> fakeCache)
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
}