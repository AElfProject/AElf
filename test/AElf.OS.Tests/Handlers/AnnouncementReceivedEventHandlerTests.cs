using System.Threading.Tasks;
using AElf.OS.BlockSync.Domain;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class AnnouncementReceivedEventHandlerTests : OSTestBase
    {
        private readonly AnnouncementReceivedEventHandler _announcementReceivedEventHandler;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public AnnouncementReceivedEventHandlerTests()
        {
            _announcementReceivedEventHandler = GetRequiredService<AnnouncementReceivedEventHandler>();
            _blockDownloadJobStore = GetRequiredService<IBlockDownloadJobStore>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var eventData = new AnnouncementReceivedEventData(new BlockAnnouncement
                {
                    BlockHash = HashHelper.ComputeFrom("BlockHash"),
                    BlockHeight = 100
                }, "Pubkey"
            );
            
            await HandleEventAsync(eventData);
            var job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldNotBeNull();
            job.TargetBlockHash.ShouldBe(eventData.Announce.BlockHash);
            job.TargetBlockHeight.ShouldBe(eventData.Announce.BlockHeight);
            job.SuggestedPeerPubkey.ShouldBe(eventData.SenderPubKey);

            await HandleEventAsync(eventData);
            job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldBeNull();
        }
        
        private async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await _announcementReceivedEventHandler.HandleEventAsync(eventData);
            await Task.Delay(500);
        }
    }
}