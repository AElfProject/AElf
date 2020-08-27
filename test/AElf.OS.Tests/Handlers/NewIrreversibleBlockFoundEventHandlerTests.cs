using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Moq;
using Xunit;

namespace AElf.OS.Handlers
{
    public class NewIrreversibleBlockFoundEventHandlerTests : NetworkBroadcastTestBase
    {
        private readonly NetworkServicePropagationTestContext _testContext;
        private readonly NewIrreversibleBlockFoundEventHandler _newIrreversibleBlockFoundEventHandler;
        private readonly INodeSyncStateProvider _syncStateProvider;

        public NewIrreversibleBlockFoundEventHandlerTests()
        {
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _newIrreversibleBlockFoundEventHandler = GetRequiredService<NewIrreversibleBlockFoundEventHandler>();
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var eventData = new NewIrreversibleBlockFoundEvent
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 100,
                PreviousIrreversibleBlockHash = HashHelper.ComputeFrom("PreviousIrreversibleBlockHash"),
                PreviousIrreversibleBlockHeight = 80
            };
            
            await HandleEventAsync(eventData);
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueLibAnnouncement(
                    It.Is<LibAnnouncement>(a => a.LibHash == eventData.BlockHash && a.LibHeight == eventData.BlockHeight),
                    It.IsAny<Action<NetworkException>>()), Times.Never);
            
            _syncStateProvider.SetSyncTarget(-1);
            
            await HandleEventAsync(eventData);
            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueLibAnnouncement(
                    It.Is<LibAnnouncement>(a => a.LibHash == eventData.BlockHash && a.LibHeight == eventData.BlockHeight),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }

        private async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _newIrreversibleBlockFoundEventHandler.HandleEventAsync(eventData);
            await Task.Delay(500);
        }

    }
}