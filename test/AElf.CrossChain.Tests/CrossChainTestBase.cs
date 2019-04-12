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

        public CrossChainTestBase()
        {
            MultiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
            CrossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            CrossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
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
                CrossChainDataConsumer.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
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
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;

        protected CrossChainWithChainTestBase()
        {
            _crossChainDataProducer = Application.ServiceProvider.GetRequiredService<ICrossChainDataProducer>();
            _crossChainDataConsumer = Application.ServiceProvider.GetRequiredService<ICrossChainDataConsumer>();
        }
        
        protected void AddFakeCacheData(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                _crossChainDataConsumer.TryRegisterNewChainCache(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    _crossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }
}