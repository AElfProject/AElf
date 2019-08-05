using System.Threading.Tasks;
using AElf.CrossChain.Communication.Grpc;
using Xunit;


namespace AElf.CrossChain.Communication
{
    public class CrossChainPluginTest : CrossChainCommunicationTestBase
    {
        private readonly CrossChainPlugin _crossChainPlugin;
        private IGrpcCrossChainServer _server;

        public CrossChainPluginTest()
        {
            _crossChainPlugin = GetRequiredService<CrossChainPlugin>();
            _server = GetRequiredService<IGrpcCrossChainServer>();
        }

        [Fact]
        public async Task GetChainInitializationData_Test()
        {
            var localChainId = ChainHelper.GetChainId(1);
            var remoteChainId = _chainOptions.ChainId;
            await _server.StartAsync("localhost", 5000); 
            CreateAndGetClient(localChainId, false, remoteChainId);
            var res = await _crossChainPlugin.GetChainInitializationDataAsync(localChainId);
            Assert.True(res.CreationHeightOnParentChain.Equals(1));
        }
    }
}