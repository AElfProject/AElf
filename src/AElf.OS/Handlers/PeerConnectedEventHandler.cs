using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        private readonly IBlockchainService _blockchainService;

        private readonly ITaskQueueManager _taskQueueManager;

        private readonly BlockSyncJob _blockSyncJob;

        public PeerConnectedEventHandler(IServiceProvider serviceProvider, ITaskQueueManager taskQueueManager,
            IBlockchainService blockchainService)
        {
            _taskQueueManager = taskQueueManager;
            _blockSyncJob = serviceProvider.GetRequiredService<BlockSyncJob>();
            _blockchainService = blockchainService;
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await ProcessNewBlock(eventData, eventData.SenderPubKey);
        }

        private async Task ProcessNewBlock(AnnouncementReceivedEventData header, string senderPubKey)
        {
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

            _taskQueueManager.Enqueue(async () =>
            {
                await _blockSyncJob.ExecuteAsync(new BlockSyncJobArgs
                {
                    SuggestedPeerPubKey = senderPubKey,
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                });
            }, OSConsts.BlockSyncQueueName);
        }

        private bool VerifyAnnouncement(PeerNewBlockAnnouncement announcement)
        {
            var allowedFutureBlockTime = DateTime.UtcNow + KernelConsts.AllowedFutureBlockTimeSpan;
            if (allowedFutureBlockTime < announcement.BlockTime.ToDateTime())
            {
                Logger.LogWarning($"Receive future block {announcement}");
                return false;
            }

            return true;
        }
    }
}