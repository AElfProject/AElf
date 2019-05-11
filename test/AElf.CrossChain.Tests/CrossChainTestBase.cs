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
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;
        protected IBlockCacheEntityConsumer BlockCacheEntityConsumer;

        public CrossChainTestBase()
        {
            ChainCacheEntityProvider = GetRequiredService<IChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
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
                    BlockCacheEntityProducer.TryAddBlockCacheEntity(blockInfo);
                }
            }
        }
    }

    public class CrossChainWithChainTestBase : AElfIntegratedTest<CrossChainWithChainTestModule>
    {
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        protected CrossChainWithChainTestBase()
        {
            _blockCacheEntityProducer = Application.ServiceProvider.GetRequiredService<IBlockCacheEntityProducer>();
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
                    _blockCacheEntityProducer.TryAddBlockCacheEntity(blockInfo);
                }
            }
        }
    }
}