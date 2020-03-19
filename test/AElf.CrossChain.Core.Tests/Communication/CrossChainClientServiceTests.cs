using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainClientServiceTests : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly CrossChainCommunicationTestHelper _crossChainCommunicationTestHelper;

        public CrossChainClientServiceTests()
        {
            _crossChainClientService = GetRequiredService<ICrossChainClientService>();
            _crossChainCommunicationTestHelper = GetRequiredService<CrossChainCommunicationTestHelper>();
        }

        [Fact]
        public async Task AddClientTest()
        {
            int remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            var crossChainClientCreationContext = new CrossChainClientCreationContext
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = "localhost",
                RemoteServerPort = 5000
            };
            _crossChainCommunicationTestHelper.SetClientConnected(remoteChainId, true);
            var client = await _crossChainClientService.CreateClientAsync(crossChainClientCreationContext);
            client.IsConnected.ShouldBeTrue();
            client.RemoteChainId.ShouldBe(remoteChainId);
        }
        
        [Fact]
        public async Task GetClientTest()
        {
            int remoteChainId1 = ChainHelper.GetChainId(1);
            int remoteChainId2 = ChainHelper.GetChainId(2);
            int remoteChainId3 = ChainHelper.GetChainId(3);
            var localChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            {
                var crossChainClientCreationContext1 = new CrossChainClientCreationContext
                {
                    LocalChainId = localChainId,
                    RemoteChainId = remoteChainId1,
                    IsClientToParentChain = false,
                    RemoteServerHost = "localhost",
                    RemoteServerPort = 5000
                };
                _crossChainCommunicationTestHelper.SetClientConnected(remoteChainId1, true);
                await _crossChainClientService.CreateClientAsync(crossChainClientCreationContext1);
                var client1 = await _crossChainClientService.GetConnectedCrossChainClientAsync(remoteChainId1);
                client1.IsConnected.ShouldBeTrue();
                client1.RemoteChainId.ShouldBe(remoteChainId1);
            }

            {
                var crossChainClientCreationContext2 = new CrossChainClientCreationContext
                {
                    LocalChainId = localChainId,
                    RemoteChainId = remoteChainId2,
                    IsClientToParentChain = false,
                    RemoteServerHost = "localhost",
                    RemoteServerPort = 5000
                };
                _crossChainCommunicationTestHelper.SetClientConnected(remoteChainId2, false);
                await _crossChainClientService.CreateClientAsync(crossChainClientCreationContext2);
                var client2 = await _crossChainClientService.GetConnectedCrossChainClientAsync(remoteChainId2);
                client2.ShouldBeNull();
            }

            {
                var client3 = await _crossChainClientService.GetConnectedCrossChainClientAsync(remoteChainId3);
                client3.ShouldBeNull();
            }
        }
    }
}