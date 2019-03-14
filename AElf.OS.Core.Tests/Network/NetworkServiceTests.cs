using System.Threading.Tasks;
using AElf.Common;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Xunit;

namespace AElf.OS.Network
{
    public class NetworkServiceTests : OSCoreNetworkServiceTestBase
    {
        private readonly INetworkService _networkService;

        public NetworkServiceTests()
        {
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task GetBlocks_FromAnyone_ReturnsBlocks()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("blocks"), 5);
            Assert.NotNull(blocks);
            Assert.True(blocks.Count == 2);
        }
        
        [Fact]
        public async Task GetBlocks_FromAnyoneThatNoOneHas_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("unknown"), 5);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlocks_SinglePeerWithNoBlocks_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 5, "p1", false);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlocks_TryOthers_ReturnsBlocks()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 5, "p1", true);
            Assert.NotNull(blocks);
            Assert.True(blocks.Count == 1);
        }
        
        [Fact]
        public async Task GetBlocks_FromUnknownPeer_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 5, "a");
            Assert.Null(blocks);
            
            // even with try others it should return null
            var blocks2 = await _networkService.GetBlocksAsync(Hash.FromString("block"), 5, "a", true);
            Assert.Null(blocks2);
        }
    }
}