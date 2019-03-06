using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.TestBase;
using Google.Protobuf;
using Moq;
using Shouldly;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ITransactionResultQueryService TransactionResultQueryService;
        protected ITransactionResultService TransactionResultService;
        protected IMultiChainBlockInfoCacheProvider MultiChainBlockInfoCacheProvider;
        protected ICrossChainDataProducer CrossChainDataProducer;
        protected ContractTester ContractTester;
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