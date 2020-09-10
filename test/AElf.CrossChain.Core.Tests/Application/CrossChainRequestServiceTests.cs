using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Application
{
    public class CrossChainRequestServiceTests : AElfIntegratedTest<CrossChainCommunicationTestModule>
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly ICrossChainClientService _crossChainClientService;
        
        public CrossChainRequestServiceTests()
        {
            _crossChainRequestService = GetRequiredService<ICrossChainRequestService>();
            _crossChainCacheEntityService = GetRequiredService<ICrossChainCacheEntityService>();
            _crossChainClientService = GetRequiredService<ICrossChainClientService>();
        }

        [Fact]
        public async Task RequestCrossChainDataTest()
        {
            var crossChainClientCreationContext = new CrossChainClientCreationContext
            {
                LocalChainId = 0,
                RemoteChainId = 1,
                IsClientToParentChain = false,
                RemoteServerHost = "localhost",
                RemoteServerPort = 5000
            };
            var communicationHelper = GetRequiredService<CrossChainCommunicationTestHelper>();
            communicationHelper.SetClientConnected(crossChainClientCreationContext.RemoteChainId, true);
            await _crossChainClientService.CreateClientAsync(crossChainClientCreationContext);
            _crossChainCacheEntityService.RegisterNewChain(crossChainClientCreationContext.RemoteChainId, 10);
            communicationHelper.CheckClientConnected(-1).ShouldBeFalse();
            await _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
            communicationHelper.CheckClientConnected(-1).ShouldBeTrue();
            
            var chainInitializationData = await _crossChainRequestService.RequestChainInitializationDataAsync(crossChainClientCreationContext.RemoteChainId);
            chainInitializationData.CreationHeightOnParentChain.ShouldBe(1);
        }
    }
}