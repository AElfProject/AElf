using System;
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
        
        /** GetBlocks **/

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
        
        /** GetBlockByHash **/
        
        [Fact]
        public async Task GetBlockByHash_UnfindablePeer_ReturnsNull()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), "a", false);
            Assert.Null(block);
        }

        [Fact]
        public async Task GetBlockByHash_WithNoPeerAndTryOthers_ShoudlThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), null, true));
        }
        
        [Fact]
        public async Task GetBlockByHash_FromSpecifiedPeer_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"),  "p1");
            Assert.NotNull(block);
        }
        
        [Fact]
        public async Task GetBlockByHash_TryOthers_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "p1", true);
            Assert.NotNull(block);
        }
        
        [Fact]
        public async Task GetBlockByHash_NoTryOthers_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "p1", false);
            Assert.Null(block);
        }
        
//        [Fact]
//        public async Task GetBlockByHash_FromUnknownPeer_ReturnsNull()
//        {
//            var blocks = await _networkService.GetBlockByHashAsync(Hash.FromString("block"), 5, "a");
//            Assert.Null(blocks);
//            
//            // even with try others it should return null
//            var blocks2 = await _networkService.GetBlocksAsync(Hash.FromString("block"), 5, "a", true);
//            Assert.Null(blocks2);
//        }
    }
}