using System.Threading.Tasks;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly NetworkOptions _networkOptions;

        public PeerConnectedEventHandler(IBlockSyncService blockSyncService,
            IBlockSyncValidationService blockSyncValidationService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _blockSyncService = blockSyncService;
            _blockSyncValidationService = blockSyncValidationService;
            _networkOptions = networkOptions.Value;
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

        //TODO: need to directly test ProcessNewBlockAsync, or unit test cannot catch exceptions of ProcessNewBlockAsync

        public Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var _ = ProcessNewBlockAsync(eventData.Announce, eventData.SenderPubKey);
            return Task.CompletedTask;
        }

        private async Task ProcessNewBlockAsync(PeerNewBlockAnnouncement announcement, string senderPubKey)
        {
            if (!await _blockSyncValidationService.ValidateBeforeEnqueue(announcement.BlockHash,
                announcement.BlockHeight))
            {
                return;
            }

            _blockSyncService.EnqueueSyncBlockJob(new SyncBlockDto
            {
                SyncBlockHash = announcement.BlockHash,
                SyncBlockHeight = announcement.BlockHeight,
                SuggestedPeerPubKey = senderPubKey,
                BatchRequestBlockCount = _networkOptions.BlockIdRequestCount,
                SyncRetryTimes = 3
            });
        }
    }
}