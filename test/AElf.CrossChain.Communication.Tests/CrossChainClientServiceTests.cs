using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainClientServiceTests : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private const int ChainId = 1234;
        public CrossChainClientServiceTests()
        {
            _crossChainClientService = GetRequiredService<ICrossChainClientService>();
        }
        
        [Fact]
        public async Task RequestChainInitializationData_Test()
        {
           var initializationData = await _crossChainClientService.RequestChainInitializationData(ChainId);
           initializationData.ShouldNotBeNull();
           initializationData.CreationHeightOnParentChain.ShouldBe(1);
        }
    }
}