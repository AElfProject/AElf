using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockFetchServiceAbnormalPeerTests : BlockSyncAbnormalPeerTestBase
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly INetworkService _networkService;

        public BlockFetchServiceAbnormalPeerTests()
        {
            _blockFetchService = GetRequiredService<IBlockFetchService>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task FetchBlock_ReturnInvalidBlock()
        {
            var abnormalPeerPubkey = "AbnormalPeerPubkey";
            
            var abnormalPeer = _networkService.GetPeerByPubkey(abnormalPeerPubkey);
            abnormalPeer.ShouldNotBeNull();
            
            var result =
                await _blockFetchService.FetchBlockAsync(HashHelper.ComputeFrom("AnnounceBlockHash"), 100, abnormalPeerPubkey);
            result.ShouldBeFalse();

            abnormalPeer = _networkService.GetPeerByPubkey(abnormalPeerPubkey);
            abnormalPeer.ShouldBeNull();
        }
    }
}