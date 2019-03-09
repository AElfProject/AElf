using System.Collections.Generic;
using System.Linq;
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
                CrossChainDataConsumer.RegisterNewChainCache(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    CrossChainDataProducer.AddNewBlockInfo(blockInfo);
                }
            }
        }
    }
}