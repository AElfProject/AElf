using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync;
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
    public class AnnouncementReceivedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>, ITransientDependency
    {
        public ILogger<AnnouncementReceivedEventHandler> Logger { get; set; }

        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockchainService _blockchainService;
        private readonly BlockSyncOptions _blockSyncOptions;

        public AnnouncementReceivedEventHandler(IBlockSyncService blockSyncService,
            IBlockSyncValidationService blockSyncValidationService,
            IBlockchainService blockchainService,
            IOptionsSnapshot<BlockSyncOptions> blockSyncOptions)
        {
            _blockSyncService = blockSyncService;
            _blockSyncValidationService = blockSyncValidationService;
            _blockchainService = blockchainService;
            _blockSyncOptions = blockSyncOptions.Value;
            
            Logger = NullLogger<AnnouncementReceivedEventHandler>.Instance;
        }

        //TODO: need to directly test ProcessNewBlockAsync, or unit test cannot catch exceptions of ProcessNewBlockAsync

        public Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var _ = ProcessNewBlockAsync(eventData.Announce, eventData.SenderPubKey);
            return Task.CompletedTask;
        }

        private async Task ProcessNewBlockAsync(BlockAnnouncement blockAnnouncement, string senderPubkey)
        {
            Logger.LogDebug(
                $"Start block sync job, target height: {blockAnnouncement.BlockHeight}, target block hash: {blockAnnouncement.BlockHash}, peer: {senderPubkey}");
            
            var chain = await _blockchainService.GetChainAsync();
            
            if (!await _blockSyncValidationService.ValidateAnnouncementAsync(chain, blockAnnouncement))
            {
                return;
            }

            await _blockSyncService.SyncByAnnouncementAsync(chain, new SyncAnnouncementDto
            {
                SyncBlockHash = blockAnnouncement.BlockHash,
                SyncBlockHeight = blockAnnouncement.BlockHeight,
                SuggestedPeerPubkey = senderPubkey,
                BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount
            });
        }
    }
}