using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandlerTests : BlockSyncTestBase
    {
        private readonly PeerConnectedEventHandler _peerConnectedEventHandler;
        private readonly BlockSyncTestHelper _blockSyncTestHelper;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;

        public PeerConnectedEventHandlerTests()
        {
            _peerConnectedEventHandler = GetRequiredService<PeerConnectedEventHandler>();
            _blockSyncTestHelper = GetRequiredService<BlockSyncTestHelper>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
        }

        [Fact]
        public async Task HandleEventTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            var peerBlocks = await _networkService.GetBlocksAsync(chain.BestChainHash, 20);

            var block = peerBlocks[0];
            var announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            };

            // Sync one block to best chain
            await _peerConnectedEventHandler.HandleEventAsync(
                new AnnouncementReceivedEventData(announcement, null));

            // Handle the same announcement again
            await _peerConnectedEventHandler.HandleEventAsync(
                new AnnouncementReceivedEventData(announcement, null));

            // Sync one block to best chain
            block = peerBlocks[1];
            announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            };

            await _peerConnectedEventHandler.HandleEventAsync(
                new AnnouncementReceivedEventData(announcement, null));

            // Sync higher block
            // BestChainHeight: 26
            block = peerBlocks[9];
            announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            };

            await _peerConnectedEventHandler.HandleEventAsync(
                new AnnouncementReceivedEventData(announcement, null));
            _blockSyncTestHelper.DisposeQueue();

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(peerBlocks.Last().GetHash());
            chain.BestChainHeight.ShouldBe(21);
        }

        [Fact]
        public async Task HandleEvent_BlockSyncQueueIsBusy()
        {
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHash = chain.BestChainHash;
            var bestChainHeight = chain.BestChainHeight;
            
            var peerBlocks = await _networkService.GetBlocksAsync(chain.BestChainHash, 20);

            var block = peerBlocks.First();
            var announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            };
            
            _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-5);
            await _peerConnectedEventHandler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, null));
            _blockSyncTestHelper.DisposeQueue();
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
        }
        
        [Fact]
        public async Task HandleEvent_BlockSyncAttachQueueIsBusy()
        {
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHash = chain.BestChainHash;
            var bestChainHeight = chain.BestChainHeight;
            
            var peerBlocks = await _networkService.GetBlocksAsync(chain.BestChainHash, 20);

            var block = peerBlocks.First();
            var announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            };
            
            _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime = TimestampHelper.GetUtcNow().AddSeconds(-3);
            await _peerConnectedEventHandler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, null));
            _blockSyncTestHelper.DisposeQueue();
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
        }
    }
}