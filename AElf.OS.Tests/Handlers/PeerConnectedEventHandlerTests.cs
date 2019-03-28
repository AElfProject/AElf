using System.Threading.Tasks;
using AElf.Common;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Volo.Abp.BackgroundJobs;
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

        /// <summary>
        /// Default amount of jobs to get from the store, since for now all tests should queues at most one, then
        /// 2 is enough to check.
        /// </summary>
        private const int MaxJobsToCheck = 2;

        private readonly PeerConnectedEventHandler _handler;
        private IBackgroundJobStore _jobStore;

        public PeerConnectedEventHandlerTests()
        {
            _handler = GetRequiredService<PeerConnectedEventHandler>();
            _jobStore = GetRequiredService<IBackgroundJobStore>();
        }

        [Fact]
        public async Task HandleEventAsync_UnderLIBHeight_DoesNothing()
        {
            var announcement = new PeerNewBlockAnnouncement {BlockHash = Hash.FromString("block"), BlockHeight = 1};
            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1"));

            var jobs = await _jobStore.GetWaitingJobsAsync(MaxJobsToCheck);
            Assert.True(jobs.Count == 0);
        }
    }
}