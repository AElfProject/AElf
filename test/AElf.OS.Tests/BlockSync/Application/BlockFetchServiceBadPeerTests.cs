using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockFetchServiceBadPeerTests : BlockSyncBadPeerTestBase
    {
        private readonly IBlockFetchService _blockFetchService;
        private readonly INetworkService _networkService;

        public BlockFetchServiceBadPeerTests()
        {
            _blockFetchService = GetRequiredService<IBlockFetchService>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task FetchBlock_ReturnInvalidBlock()
        {
            var badPeerPubkey = "BadPeerPubkey";
            
            var badPeer = _networkService.GetPeerByPubkey(badPeerPubkey);
            badPeer.ShouldNotBeNull();
            
            var result =
                await _blockFetchService.FetchBlockAsync(Hash.FromString("AnnounceBlockHash"), 100, badPeerPubkey);
            result.ShouldBeFalse();

            badPeer = _networkService.GetPeerByPubkey(badPeerPubkey);
            badPeer.ShouldBeNull();
        }
    }
}