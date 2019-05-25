using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        private readonly IBlockchainService _blockchainService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockSyncService _blockSyncService;
        private readonly NetworkOptions _networkOptions;
        
        private readonly Duration _blockSyncAnnouncementAgeLimit = new Duration {Seconds = 4};
        private readonly Duration _blockSyncAttachBlockAgeLimit = new Duration {Seconds = 2};

        public PeerConnectedEventHandler(ITaskQueueManager taskQueueManager,
            IBlockchainService blockchainService,
            IBlockSyncService blockSyncService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _blockSyncService = blockSyncService;
            _networkOptions = networkOptions.Value;
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await ProcessNewBlock(eventData, eventData.SenderPubKey);
        }

        private async Task ProcessNewBlock(AnnouncementReceivedEventData header, string senderPubKey)
        {
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

            var blockHeight = header.Announce.BlockHeight;
            var blockHash = header.Announce.BlockHash;

            Logger.LogTrace($"Receive header {{ hash: {blockHash}, height: {blockHeight} }} from {senderPubKey}.");

            if (!VerifyAnnouncement(header.Announce))
            {
                return;
            }

            var chain = await _blockchainService.GetChainAsync();
            if (blockHeight < chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {blockHash}, height: {blockHeight} }} " +
                                $"form {senderPubKey}, ignore.");
                return;
            }

            var enqueueTimestamp = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                try
                {
                    _blockSyncService.SetBlockSyncAnnouncementEnqueueTime(enqueueTimestamp);
                    await _blockSyncService.SyncBlockAsync(blockHash, blockHeight, _networkOptions.BlockIdRequestCount,
                        senderPubKey);
                }
                finally
                {
                    _blockSyncService.SetBlockSyncAnnouncementEnqueueTime(null);
                }
            }, OSConsts.BlockSyncQueueName);
        }

        private bool VerifyAnnouncement(PeerNewBlockAnnouncement announcement)
        {
            var allowedFutureBlockTime = DateTime.UtcNow + KernelConstants.AllowedFutureBlockTimeSpan;
            if (allowedFutureBlockTime < announcement.BlockTime.ToDateTime())
            {
                Logger.LogWarning($"Receive future block {announcement}");
                return false;
            }

            return true;
        }
    }
}