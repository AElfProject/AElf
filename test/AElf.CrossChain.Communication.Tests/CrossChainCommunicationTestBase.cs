using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.TestBase;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationTestBase : AElfIntegratedTest<CrossChainCommunicationTestModule>
    {
        protected readonly ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected readonly IBlockCacheEntityProducer BlockCacheEntityProducer;
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();

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
        
        public void AddFakeParentChainIdHeight(int parentChainId, long height)
        {
            _parentChainIdHeight.Add(parentChainId, height);
        }
    }
}