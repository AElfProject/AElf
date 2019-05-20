using System.Diagnostics;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class SyncStateAnnouncementEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await ProcessAnnouncement(eventData, eventData.SenderPubKey);
        }

        private async Task ProcessAnnouncement(AnnouncementReceivedEventData eventData, string eventDataSenderPubKey)
        {
            // check how many peer know about
            // set state accordingly
        }
    }
}