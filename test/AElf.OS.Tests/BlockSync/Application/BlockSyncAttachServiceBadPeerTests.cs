using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachServiceBadPeerTests : BlockSyncAttachBlockBadPeerTestBase
    {
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly OSTestHelper _osTestHelper;
        private readonly INetworkService _networkService;
        
        public BlockSyncAttachServiceBadPeerTests()
        {
            _blockSyncAttachService = GetRequiredService<IBlockSyncAttachService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task Attach_InvalidBlock()
        {
            var badPeerPubkey = "BadPeerPubkey";
            var badBlock = _osTestHelper.GenerateBlockWithTransactions(Hash.FromString("BadBlock"), 10000);
            
            var badPeer = _networkService.GetPeerByPubkey(badPeerPubkey);
            badPeer.ShouldNotBeNull();

            await _blockSyncAttachService.AttachBlockWithTransactionsAsync(badBlock, badPeerPubkey);
            
            badPeer = _networkService.GetPeerByPubkey(badPeerPubkey);
            badPeer.ShouldBeNull();
        }
    }
}