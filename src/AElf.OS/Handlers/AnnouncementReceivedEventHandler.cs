using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class AnnouncementReceivedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>, ITransientDependency
    {
        private readonly IAnnouncementSyncService _announcementSyncService;
        private readonly IBlockchainService _blockchainService;
        private readonly NetworkOptions _networkOptions;

        public AnnouncementReceivedEventHandler(IAnnouncementSyncService announcementSyncService, 
            IBlockchainService blockchainService, IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _announcementSyncService = announcementSyncService;
            _blockchainService = blockchainService;
            _networkOptions = networkOptions.Value;
        }
        
        public Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var _ = ProcessNewBlockAsync(eventData.Announce, eventData.SenderPubKey);
            return Task.CompletedTask;
        }

        private async Task ProcessNewBlockAsync(BlockAnnouncement blockAnnouncement, string senderPubkey)
        {
            var chain = await _blockchainService.GetChainAsync();
            await _announcementSyncService.SyncByAnnouncementAsync(chain, blockAnnouncement, senderPubkey, _networkOptions.BlockIdRequestCount);
        }
    }
}