using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>, ITransientDependency
    {
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockchainService _blockchainService;
        private readonly NetworkOptions _networkOptions;

        public PeerConnectedEventHandler(IBlockSyncService blockSyncService,
            IBlockSyncValidationService blockSyncValidationService,
            IBlockchainService blockchainService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _blockSyncService = blockSyncService;
            _blockSyncValidationService = blockSyncValidationService;
            _blockchainService = blockchainService;
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
            Logger.LogDebug(
                $"Start block sync job, target height: {announcement.BlockHeight}, target block hash: {announcement.BlockHash}, peer: {senderPubKey}");
            
            var chain = await _blockchainService.GetChainAsync();
            
            if (!await _blockSyncValidationService.ValidateBeforeSync(chain, announcement.BlockHash,
                announcement.BlockHeight))
            {
                return;
            }

            await _blockSyncService.SyncBlockAsync(chain, new SyncBlockDto
            {
                SyncBlockHash = announcement.BlockHash,
                SyncBlockHeight = announcement.BlockHeight,
                SuggestedPeerPubKey = senderPubKey,
                BatchRequestBlockCount = _networkOptions.BlockIdRequestCount
            });
        }
    }
}