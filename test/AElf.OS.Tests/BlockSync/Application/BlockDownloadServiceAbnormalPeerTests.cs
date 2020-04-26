using System.Threading.Tasks;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockDownloadServiceAbnormalPeerTests : BlockSyncAbnormalPeerTestBase
    {
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly INetworkService _networkService;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        
        public BlockDownloadServiceAbnormalPeerTests()
        {
            _blockDownloadService = GetRequiredService<IBlockDownloadService>();
            _networkService = GetRequiredService<INetworkService>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
        }

        [Fact]
        public async Task DownloadBlock_NotLinkedBlocks()
        {
            var notLinkedBlockPubkey = "NotLinkedBlockPubkey";
            
            var abnormalPeer = _networkService.GetPeerByPubkey(notLinkedBlockPubkey);
            abnormalPeer.ShouldNotBeNull();

            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = notLinkedBlockPubkey,
                UseSuggestedPeer = true
            });
            result.Success.ShouldBeFalse();

            abnormalPeer = _networkService.GetPeerByPubkey(notLinkedBlockPubkey);
            abnormalPeer.ShouldBeNull();
        }
        
        [Fact]
        public async Task DownloadBlock_WrongLibHash()
        {
            var wrongLIBPubkey = "WrongLIBPubkey";

            _blockSyncStateProvider.LastRequestPeerPubkey = wrongLIBPubkey;

            var abnormalPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            abnormalPeer.ShouldNotBeNull();
            
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                PreviousBlockHeight = 100,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "GoodPeerPubkey0",
                UseSuggestedPeer = false
            });
            
            result.Success.ShouldBeTrue();
            result.DownloadBlockCount.ShouldBe(0);
            
            abnormalPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            abnormalPeer.ShouldBeNull();
        }

        [Fact]
        public async Task DownloadBlock_WrongLibHash_NotEnoughPeer()
        {
            var wrongLIBPubkey = "WrongLIBPubkey";

            _blockSyncStateProvider.LastRequestPeerPubkey = wrongLIBPubkey;

            var abnormalPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            abnormalPeer.ShouldNotBeNull();
            
            var result = await _blockDownloadService.DownloadBlocksAsync(new DownloadBlockDto
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                PreviousBlockHeight = 155,
                BatchRequestBlockCount = 5,
                MaxBlockDownloadCount = 5,
                SuggestedPeerPubkey = "GoodPeerPubkey14",
                UseSuggestedPeer = false
            });
            
            result.Success.ShouldBeTrue();
            result.DownloadBlockCount.ShouldBe(0);
            
            abnormalPeer = _networkService.GetPeerByPubkey(wrongLIBPubkey);
            abnormalPeer.ShouldNotBeNull();
        }
    }
}