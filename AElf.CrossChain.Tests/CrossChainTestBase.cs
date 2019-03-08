using System.Collections.Generic;
using AElf.CrossChain.Cache;
using AElf.Kernel.Blockchain.Application;
using AElf.TestBase;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ITransactionResultQueryService TransactionResultQueryService;
        protected ITransactionResultService TransactionResultService;
        protected IMultiChainBlockInfoCacheProvider MultiChainBlockInfoCacheProvider;
        protected ICrossChainDataProducer CrossChainDataProducer;
        protected ICrossChainDataConsumer CrossChainDataConsumer;

        public CrossChainTestBase()
        {
            TransactionResultQueryService = GetRequiredService<ITransactionResultQueryService>();
            TransactionResultService = GetRequiredService<ITransactionResultService>();
            MultiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
            CrossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            CrossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
        }

        protected IMultiChainBlockInfoCacheProvider CreateFakeMultiChainBlockInfoCacheProvider(
            Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            var multiChainBlockInfoCacheProvider = new MultiChainBlockInfoCacheProvider();
            foreach (var (chainId, blockInfos) in fakeCache)
            {
                var blockInfoCache = new BlockInfoCache(1);
                multiChainBlockInfoCacheProvider.AddBlockInfoCache(chainId, blockInfoCache);
                foreach (var blockInfo in blockInfos)
                {
                    blockInfoCache.TryAdd(blockInfo);
                }
            }

            return multiChainBlockInfoCacheProvider;
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
                CrossChainDataConsumer.RegisterNewChainCache(crossChainId);
                foreach (var blockInfo in blockInfos)
                {
                    CrossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }
}