using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel.Account.Application;
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
        protected ITransactionResultManager TransactionResultManager;

        public CrossChainTestBase()
        {
            TransactionResultManager = GetRequiredService<ITransactionResultManager>();
        }
        
        protected IMultiChainBlockInfoCacheProvider CreateFakeMultiChainBlockInfoCacheProvider(Dictionary<int, List<IBlockInfo>> fakeCache)
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

        protected ICrossChainDataConsumer CreateFakeCrossCHainDataConsumer(IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            return new CrossChainDataConsumer(multiChainBlockInfoCacheProvider);            
        }

        protected ICrossChainDataProducer CreateFakeCrossChainDataProducer(
            IMultiChainBlockInfoCacheProvider multiChainBlockInfoCacheProvider)
        {
            return new CrossChainDataProducer(multiChainBlockInfoCacheProvider);
        }

        protected ICrossChainDataProvider CreateFakeCrossChainDataProvider(
            ICrossChainContractReader crossChainContractReader, ICrossChainDataConsumer crossChainDataConsumer)
        {
            return new CrossChainDataProvider(crossChainContractReader, crossChainDataConsumer);
        }
        
        protected ICrossChainContractReader CreateFakeCrossChainContractReader(Dictionary<int, ulong> sideChainIdHeights, Dictionary<int, ulong> parentCHainIdHeights)
        {
            Mock<ICrossChainContractReader> mockObject = new Mock<ICrossChainContractReader>();
            mockObject.Setup(m => m.GetSideChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns<int, int, Hash, ulong>((chainId, sideChainId, preBlockHash, preBlockHeight) => Task.FromResult<ulong>(sideChainIdHeights.ContainsKey(sideChainId) ? sideChainIdHeights[sideChainId] : 0));
            mockObject.Setup(m => m.GetParentChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult<ulong>(parentCHainIdHeights.Count > 0 ? parentCHainIdHeights.Values.FirstOrDefault() : 0));
            mockObject.Setup(m => m.GetSideChainIdAndHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(new Dictionary<int, ulong>(sideChainIdHeights)));
            mockObject.Setup(m => m.GetAllChainsIdAndHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(new Dictionary<int, ulong>(new Dictionary<int, ulong>(sideChainIdHeights).Concat(parentCHainIdHeights))));
            mockObject.Setup(m => m.GetParentChainIdAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(parentCHainIdHeights.Keys.FirstOrDefault()));
            return mockObject.Object;
        }
    }
}