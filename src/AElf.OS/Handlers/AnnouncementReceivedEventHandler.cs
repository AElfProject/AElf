using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class AnnouncementReceivedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public ILogger<AnnouncementReceivedEventHandler> Logger { get; set; }

        private readonly IBlockchainService _blockchainService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncService _blockSyncService;
        private readonly NetworkOptions _networkOptions;
        
        private readonly Duration _blockSyncAnnouncementAgeLimit = new Duration {Seconds = 4};
        private readonly Duration _blockSyncAttachBlockAgeLimit = new Duration {Seconds = 2};

        public AnnouncementReceivedEventHandler(ITaskQueueManager taskQueueManager,
            IBlockchainService blockchainService,
            IBlockSyncService blockSyncService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _blockSyncService = blockSyncService;
            _networkOptions = networkOptions.Value;
            Logger = NullLogger<AnnouncementReceivedEventHandler>.Instance;
        }

        //TODO: need to directly test ProcessNewBlockAsync, or unit test cannot catch exceptions of ProcessNewBlockAsync

        public Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var _ = ProcessNewBlockAsync(eventData, eventData.SenderPubKey);
            return Task.CompletedTask;
        }


        private async Task ProcessNewBlockAsync(AnnouncementReceivedEventData header, string senderPubKey)
        {
            Logger.LogTrace($"Receive announcement and sync block {{ hash: {header.Announce.BlockHash}, height: {header.Announce.BlockHeight} }} from {senderPubKey}.");

            var announcementEnqueueTime = _blockSyncService.GetBlockSyncAnnouncementEnqueueTime();
            if (announcementEnqueueTime != null &&
                TimestampHelper.GetUtcNow() > announcementEnqueueTime + _blockSyncAnnouncementAgeLimit)
            {
                Logger.LogWarning(
                    $"Block sync queue is too busy, enqueue timestamp: {announcementEnqueueTime.ToDateTime()}");
                return;
            }
            
            var blockSyncAttachBlockEnqueueTime = _blockSyncService.GetBlockSyncAttachBlockEnqueueTime();
            if (blockSyncAttachBlockEnqueueTime != null &&
                TimestampHelper.GetUtcNow() >
                blockSyncAttachBlockEnqueueTime + _blockSyncAttachBlockAgeLimit)
            {
                Logger.LogWarning(
                    $"Block sync attach queue is too busy, enqueue timestamp: {blockSyncAttachBlockEnqueueTime.ToDateTime()}");
                return;
            }
            
            var chain = await _blockchainService.GetChainAsync();
            if (header.Announce.BlockHeight < chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {header.Announce.BlockHash}, height: {header.Announce.BlockHeight} }} " +
                                $"form {senderPubKey}, ignore.");
                return;
            }

            EnqueueJob(header, senderPubKey);
        }

        private void EnqueueJob(AnnouncementReceivedEventData header, string senderPubKey)
        {
            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    _blockSyncService.SetBlockSyncAnnouncementEnqueueTime(enqueueTimestamp);
                    await _blockSyncService.SyncBlockAsync(header.Announce.BlockHash, header.Announce.BlockHeight,
                        _networkOptions.BlockIdRequestCount, senderPubKey);
                }
                finally
                {
                    _blockSyncService.SetBlockSyncAnnouncementEnqueueTime(null);
                }
            }, OSConsts.BlockSyncQueueName);
        }        
    }
}