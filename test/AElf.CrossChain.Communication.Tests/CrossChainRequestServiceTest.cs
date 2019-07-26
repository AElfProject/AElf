using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Application;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainRequestServiceTest : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainRequestService _chainRequestService;

        public CrossChainRequestServiceTest()
        {
            _chainRequestService = GetRequiredService<ICrossChainRequestService>();
        }

        [Fact]
        public async Task RequestCrossChainDataFromOtherChains()
        {
            int chainId = 123;

            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new ParentChainBlockData {ChainId = chainId, Height = 1}
            };
            AddFakeParentChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            await _chainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
    }
}