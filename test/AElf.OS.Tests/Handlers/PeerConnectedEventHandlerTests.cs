using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandlerTests : SyncTestBase
    {
        // Cases to test:
        // 1) we already have the block
        // 2) we don't have the block => go get it:
        //     a) It's linkable => nothing happens
        //     b) It's not linkable => queue job

        private readonly PeerConnectedEventHandler _handler;

        public PeerConnectedEventHandlerTests()
        {
            _handler = GetRequiredService<PeerConnectedEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync_UnderLIBHeight_DoesNothing()
        {
            var announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = Hash.FromString("block"),
                BlockHeight = 1,
                BlockTime = TimestampHelper.GetUtcNow()
            };
            var exception = await Record.ExceptionAsync(async () =>
                await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1")));
            Assert.Null(exception);
        }

        [Fact]
        public async Task HandleEventAsync_FutureBlock_DoesNothing()
        {
            var announcement = new PeerNewBlockAnnouncement
            {
                BlockHash = Hash.FromString("block"),
                BlockHeight = 1,
                BlockTime = TimestampHelper.GetUtcNow().AddSeconds(5)
            };
            var exception = await Record.ExceptionAsync(async () => 
                await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1")));
            Assert.Null(exception);
        }
    }
}