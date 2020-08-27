using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockAcceptedEventHandlerTests : NetworkBroadcastTestBase
    {
        private readonly BlockAcceptedEventHandler _blockAcceptedEventHandler;
        private readonly OSTestHelper _osTestHelper;
        private readonly NetworkServicePropagationTestContext _testContext;
        private readonly INodeSyncStateProvider _syncStateProvider;

        public BlockAcceptedEventHandlerTests()
        {
            _blockAcceptedEventHandler = GetRequiredService<BlockAcceptedEventHandler>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var block = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("BlockHash"), 20);

            await HandleEventAsync(block);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == block.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);
            _syncStateProvider.SyncTarget.ShouldBe(0);

            _syncStateProvider.SetSyncTarget(21);
            await HandleEventAsync(block);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == block.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);

            _syncStateProvider.SetSyncTarget(10);
            await HandleEventAsync(block);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == block.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Never);
            _syncStateProvider.SyncTarget.ShouldBe(-1);

            await HandleEventAsync(block);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueAnnouncement(It.Is<BlockAnnouncement>(ba => ba.BlockHash == block.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once);
        }

        private async Task HandleEventAsync(Block block)
        {
            await _blockAcceptedEventHandler.HandleEventAsync(new BlockAcceptedEvent
                {BlockExecutedSet = new BlockExecutedSet {Block = block}});
            await Task.Delay(500);
        }
    }
}