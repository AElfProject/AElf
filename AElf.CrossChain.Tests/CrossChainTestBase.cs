using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.TestBase;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ITransactionResultQueryService TransactionResultQueryService;
        protected ITransactionResultService TransactionResultService;
        protected IMultiChainBlockInfoCacheProvider MultiChainBlockInfoCacheProvider;

        public CrossChainTestBase()
        {
            TransactionResultQueryService = GetRequiredService<ITransactionResultQueryService>();
            TransactionResultService = GetRequiredService<ITransactionResultService>();
            MultiChainBlockInfoCacheProvider = GetRequiredService<IMultiChainBlockInfoCacheProvider>();
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

        protected ICrossChainDataConsumer CreateFakeCrossChainDataConsumer(
            IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            return new CrossChainDataConsumer(multiChainBlockInfoCacheProvider);
        }

        protected void CreateFakeCacheData(Dictionary<int, BlockInfoCache> cachingData)
        {
            foreach (var (key, value) in cachingData)
            {
                MultiChainBlockInfoCacheProvider.AddBlockInfoCache(key, value);
            }
        }
        protected ICrossChainContractReader CreateFakeCrossChainContractReader(
            Dictionary<int, long> sideChainIdHeights, Dictionary<int, long> parentChainIdHeights)
        {
            Mock<ICrossChainContractReader> mockObject = new Mock<ICrossChainContractReader>();
//            mockObject.Setup(
//                    m => m.GetSideChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<long>()))
//                .Returns<int, Hash, long>((sideChainId, preBlockHash, preBlockHeight) =>
//                    Task.FromResult<long>(sideChainIdHeights.ContainsKey(sideChainId)
//                        ? sideChainIdHeights[sideChainId]
//                        : 0));
//            mockObject.Setup(m => m.GetParentChainCurrentHeightAsync(It.IsAny<Hash>(), It.IsAny<long>()))
//                .Returns(Task.FromResult<long>(parentChainIdHeights.Count > 0
//                    ? parentChainIdHeights.Values.FirstOrDefault()
//                    : 0));
//            mockObject.Setup(m => m.GetSideChainIdAndHeightAsync(It.IsAny<Hash>(), It.IsAny<long>()))
//                .Returns(Task.FromResult(new Dictionary<int, long>(sideChainIdHeights)));
//            mockObject.Setup(m => m.GetAllChainsIdAndHeightAsync(It.IsAny<Hash>(), It.IsAny<long>()))
//                .Returns(Task.FromResult(
//                    new Dictionary<int, long>(
//                        new Dictionary<int, long>(sideChainIdHeights).Concat(parentChainIdHeights))));
//            mockObject.Setup(m => m.GetParentChainIdAsync(It.IsAny<Hash>(), It.IsAny<long>()))
//                .Returns(Task.FromResult(parentChainIdHeights.Keys.FirstOrDefault()));
            return mockObject.Object;
        }
    }
}