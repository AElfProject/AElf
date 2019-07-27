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
        public async Task StartAsync()
        {
            int chainId = 123;
            await _crossChainPlugin.StartAsync(chainId);
        }

        [Fact]
        public async Task GetChainInitializationData()
        {
            int chainId = 123;
            var res = await _crossChainPlugin.GetChainInitializationDataAsync(chainId);
            Assert.True(res.CreationHeightOnParentChain.Equals(1));
        }
    }
}