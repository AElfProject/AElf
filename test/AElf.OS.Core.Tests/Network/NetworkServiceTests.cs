using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Moq;
using Xunit;

namespace AElf.OS.Network
{
    public class NetworkServiceTests : OSCoreNetworkServiceTestBase
    {
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;

        public NetworkServiceTests()
        {
            _networkService = GetRequiredService<INetworkService>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        #region GetBlocks

        [Fact]
        public async Task GetBlocks_FromAnyone_ReturnsBlocks()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("blocks"), 0, 5);
            Assert.NotNull(blocks);
            Assert.True(blocks.Count == 2);
        }
        
        [Fact]
        public async Task GetBlocks_FromAnyoneThatNoOneHas_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("unknown"), 0, 5);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlocks_SinglePeerWithNoBlocks_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 0, 5, "p1", false);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlocks_TryOthers_ReturnsBlocks()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 0, 5, "p1", true);
            Assert.NotNull(blocks);
            Assert.True(blocks.Count == 1);
        }
        
        [Fact]
        public async Task GetBlocks_FromUnknownPeer_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("block"), 0, 5, "a");
            Assert.Null(blocks);
            
            // even with try others it should return null
            var blocks2 = await _networkService.GetBlocksAsync(Hash.FromString("block"), 0, 5, "a", true);
            Assert.Null(blocks2);
        }
        
        [Fact]
        public async Task GetBlocks_FaultyPeer_ReturnsNull()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "failed_peer");
            Assert.Null(block);
        }

        #endregion GetBlocks

        #region GetBlockByHash

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
        public async Task GetBlocks_TryOthersRandomBlock_ReturnsBlocks()
        {
            var blocks = await _networkService.GetBlockByHashAsync(Hash.FromString("rnd"), "p1", true);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlockByHash_NoTryOthers_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "p1", false);
            Assert.Null(block);
        }
        
        [Fact]
        public async Task GetBlockByHash_FromUnknownPeer_ReturnsNull()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "failed_peer");
            Assert.Null(block);
        }

        #endregion GetBlockByHash

        #region Broadcasts

        [Fact]
        public async Task BroadcastAnnounceAsync_OnePeerThrows_ShouldNotBlockOthers()
        {
            int successfulBcasts = await _networkService.BroadcastAnnounceAsync(new BlockHeader());
            Assert.Equal(successfulBcasts, _peerPool.GetPeers().Count-1);
        }
        
        [Fact]
        public async Task BroadcastTransactionAsync_OnePeerThrows_ShouldNotBlockOthers()
        {
            int successfulBcasts = await _networkService.BroadcastAnnounceAsync(new BlockHeader());
            Assert.Equal(successfulBcasts, _peerPool.GetPeers().Count-1);
        }

        #endregion Broadcasts
        
        #region GetPeers

        [Fact]
        public void GetPeers_ShouldIncludeFailing()
        {
            Assert.Equal(_networkService.GetPeerIpList().Count, _peerPool.GetPeers(true).Count);
        }
        
        #endregion GetPeers
        
    }
}