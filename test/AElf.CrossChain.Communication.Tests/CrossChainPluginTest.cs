using System.Threading.Tasks;
using AElf.CrossChain.Communication.Grpc;
using Xunit;


namespace AElf.CrossChain.Communication
{
    public class CrossChainPluginTest : CrossChainCommunicationTestBase
    {
        private readonly CrossChainPlugin _crossChainPlugin;

        public CrossChainPluginTest()
        {
            _crossChainPlugin = GetRequiredService<CrossChainPlugin>();
        }

        [Fact]
        public async Task StartAsync_Test()
        {
            int remoteChainId = ChainHelper.GetChainId(1);
            int localChainId = ChainHelper.GetChainId(2);
            await _crossChainPlugin.StartAsync(localChainId);
            var client = await CreateAndGetClient(localChainId, false, 5001, remoteChainId);
            Assert.True(client.IsConnected);
            Assert.True(client.TargetUriString.Equals("localhost:5001"));
        }

        [Fact]
        public async Task GetChainInitializationData_Test()
        {
            int chainId = ChainHelper.GetChainId(1);
            var res = await _crossChainPlugin.GetChainInitializationDataAsync(chainId);
            Assert.True(res.CreationHeightOnParentChain.Equals(1));
        }
    }
}