using System.Threading.Tasks;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceRetryTests : BlockSyncRetryTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly BlockSyncRetryTestContext _blockSyncRetryTestContext;
        private readonly INetworkService _networkService;

        public BlockDownloadServiceRetryTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _blockSyncRetryTestContext = GetRequiredService<BlockSyncRetryTestContext>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task DownloadBlocks_Retry_Test()
        {
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "PeerPubkey0",
                UseSuggestedPeer = false
            });

            result.Success.ShouldBeFalse();
            result.DownloadBlockCount.ShouldBe(0);

            _blockSyncRetryTestContext.MockedNetworkService.Verify(
                s => s.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
        }
        
        [Fact]
        public async Task DownloadBlocks_Retry_OnlyOnePeer_Test()
        {
            foreach (var peer in _networkService.GetPeers(false))
            {
                peer.SyncState = SyncState.Syncing;
            }
            
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "PeerPubkey0",
                UseSuggestedPeer = false
            });

            result.Success.ShouldBeFalse();
            result.DownloadBlockCount.ShouldBe(0);

            _blockSyncRetryTestContext.MockedNetworkService.Verify(
                s => s.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
        }
    }
}