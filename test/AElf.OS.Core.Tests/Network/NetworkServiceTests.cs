using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
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
        public async Task GetBlocks_FromAnyoneThatNoOneHas_ReturnsNull()
        {
            var blocks = await _networkService.GetBlocksAsync(Hash.FromString("unknown"), 5);
            Assert.Null(blocks);
        }
        
        [Fact]
        public async Task GetBlocks_FaultyPeer_ShouldGetFromBestPeer()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash2"), "failed_peer");
            Assert.NotNull(block);
        }

        #endregion GetBlocks

        #region GetBlockByHash

        [Fact]
        public async Task GetBlockByHash_UnfindablePeer_ReturnsNull()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), "a");
            Assert.Null(block);
        }
        
        [Fact]
        public async Task GetBlockByHash_FromSpecifiedPeer_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"),  "p1");
            Assert.NotNull(block);
        }

        #endregion GetBlockByHash

        #region GetPeers

        [Fact]
        public void GetPeers_ShouldIncludeFailing()
        {
            Assert.Equal(_networkService.GetPeers().Count, _peerPool.GetPeers(true).Count);
        }
        
        #endregion GetPeers
        
    }
}