using System.Threading.Tasks;
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
            int chainId = ChainHelper.GetChainId(1);
            await _crossChainPlugin.StartAsync(chainId);
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