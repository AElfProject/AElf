using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandlerTests : BlockSyncTestBase
    {
        private readonly PeerConnectedEventHandler _peerConnectedEventHandler;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly OSTestHelper _osTestHelper;

        public PeerConnectedEventHandlerTests()
        {
            _peerConnectedEventHandler = GetRequiredService<PeerConnectedEventHandler>();
            _blockSyncStateProvider = GetRequiredService<IBlockSyncStateProvider>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
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

            {
                // Sync one block to best chain
                // BestChainHeight: 12
                await _peerConnectedEventHandler.HandleEventAsync(
                    new AnnouncementReceivedEventData(announcement, null));
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHash.ShouldBe(peerBlocks[0].GetHash());
                chain.BestChainHeight.ShouldBe(12);
            }

            {
                // Handle the same announcement again
                // BestChainHeight: 12
                await _peerConnectedEventHandler.HandleEventAsync(
                    new AnnouncementReceivedEventData(announcement, null));
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHash.ShouldBe(peerBlocks[0].GetHash());
                chain.BestChainHeight.ShouldBe(12);
            }

            Hash forkedBlockHash;
            {
                // Mined one block, and fork
                _osTestHelper.MinedOneBlock();
                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHeight.ShouldBe(13);
                forkedBlockHash = chain.BestChainHash;
            }

            {
                // Receive a higher fork block, sync from the lib
                // BestChainHeight: 21
                block = peerBlocks[9];
                announcement = new PeerNewBlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                };

                await _peerConnectedEventHandler.HandleEventAsync(
                    new AnnouncementReceivedEventData(announcement, null));

                chain = await _blockchainService.GetChainAsync();
                chain.BestChainHash.ShouldBe(peerBlocks.Last().GetHash());
                chain.BestChainHeight.ShouldBe(21);

                var block13 = await _blockchainService.GetBlockByHeightInBestChainBranchAsync(13);
                block13.GetHash().ShouldNotBe(forkedBlockHash);
            }
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
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
        }
    }
}