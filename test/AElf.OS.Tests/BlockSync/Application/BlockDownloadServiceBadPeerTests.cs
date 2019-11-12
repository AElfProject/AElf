using System.Threading.Tasks;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceBadPeerTests : BlockSyncBadPeerTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly INetworkService _networkService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        
        public BlockDownloadServiceBadPeerTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _networkService = GetRequiredService<INetworkService>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
        }

        [Fact]
        public async Task DownloadBlock_NotLinkedBlocks()
        {
            var notLinkedBlockPubkey = "NotLinkedBlockPubkey";
            
            var badPeer = _networkService.GetPeerByPubkey(notLinkedBlockPubkey);
            badPeer.ShouldNotBeNull();

            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = Hash.FromString("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = notLinkedBlockPubkey,
                UseSuggestedPeer = true
            });
            result.Success.ShouldBeFalse();

            badPeer = _networkService.GetPeerByPubkey(notLinkedBlockPubkey);
            badPeer.ShouldBeNull();
        }
        
        [Fact]
        public async Task DownloadBlock_WrongLibHash()
        {
            var wrongLIBPubkey = "WrongLIBPubkey";

            _blockSyncStateProvider.LastRequestPeerPubkey = wrongLIBPubkey;

            var badPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            badPeer.ShouldNotBeNull();
            
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = Hash.FromString("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "GoodPeerPubkey0",
                UseSuggestedPeer = false
            });
            
            result.Success.ShouldBeTrue();
            result.DownloadBlockCount.ShouldBe(0);
            
            badPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            badPeer.ShouldBeNull();
        }

        [Fact]
        public async Task DownloadBlock_WrongLibHash_NotEnoughPeer()
        {
            var wrongLIBPubkey = "WrongLIBPubkey";

            _blockSyncStateProvider.LastRequestPeerPubkey = "GoodPeerPubkey0";

            var badPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            badPeer.ShouldNotBeNull();
            
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = Hash.FromString("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "GoodPeerPubkey14",
                UseSuggestedPeer = false
            });
            
            result.Success.ShouldBeTrue();
            result.DownloadBlockCount.ShouldBe(0);
            
            badPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            badPeer.ShouldNotBeNull();
        }
    }
}