using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandlerTest : CrossChainCommunicationTestBase
    {
        private CrossChainCommunicationEventHandler _crossChainCommunicationEventHandler;

        public CrossChainCommunicationEventHandlerTest()
        {
            _crossChainCommunicationEventHandler = GetRequiredService<CrossChainCommunicationEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync_Test()
        {
            var eventData = new CrossChainDataValidatedEvent();
            int chainId = ChainHelper.GetChainId(1);

            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new ParentChainBlockData {ChainId = chainId, Height = 1}
            };
            AddFakeParentChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            await _crossChainCommunicationEventHandler.HandleEventAsync(eventData);
        }
    }
}