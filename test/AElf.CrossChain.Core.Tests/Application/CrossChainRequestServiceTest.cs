using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using Xunit;

namespace AElf.CrossChain.Application
{
    public class CrossChainRequestServiceTest : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainRequestService _chainRequestService;

        public CrossChainRequestServiceTest()
        {
            _chainRequestService = GetRequiredService<ICrossChainRequestService>();
        }

        [Fact]
        public async Task RequestCrossChainDataFromOtherChains_Test()
        {
            int chainId = ChainHelper.GetChainId(1);

            var blockInfoCache = new List<ICrossChainBlockEntity>
            {
                new ParentChainBlockData {ChainId = chainId, Height = 1}
            };
            AddFakeParentChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            var localChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            CreateAndGetClient(localChainId, false, chainId);
            await _chainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
    }
}