using System.Collections.Generic;
using System.Linq;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.TestBase;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationTestBase : AElfIntegratedTest<CrossChainCommunicationTestModule>
    {
        protected readonly ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected readonly IBlockCacheEntityProducer BlockCacheEntityProducer;
        private readonly Dictionary<int, long> _sideChainIdHeights = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();
        private readonly Dictionary<long, CrossChainBlockData> _indexedCrossChainBlockData = new Dictionary<long, CrossChainBlockData>();

        public CrossChainCommunicationTestBase()
        {
            CrossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
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

        public void AddFakeSideChainIdHeight(int sideChainId, long height)
        {
            _sideChainIdHeights.Add(sideChainId, height);
        }

        public void AddFakeParentChainIdHeight(int parentChainId, long height)
        {
            _parentChainIdHeight.Add(parentChainId, height);
        }

        internal void AddFakeIndexedCrossChainBlockData(long height, CrossChainBlockData crossChainBlockData)
        {
            _indexedCrossChainBlockData.Add(height, crossChainBlockData);
        }
    }
}