using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Jobs;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>, ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public IBlockchainService BlockchainService { get; set; }
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        public PeerConnectedEventHandler()
        {
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await ProcessNewBlock(eventData, eventData.SenderPubKey);
        }

        public async Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            //await ProcessNewBlock(eventData, eventData.Peer);
        }

        private async Task ProcessNewBlock(AnnouncementReceivedEventData header, string senderPubKey)
        {
            var blockHeight = header.Announce.BlockHeight;
            var blockHash = header.Announce.BlockHash;

            Logger.LogTrace($"Receive header {{ hash: {blockHash}, height: {blockHeight} }} from {senderPubKey}.");

            var chain = await BlockchainService.GetChainAsync();
            if (blockHeight < chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {blockHash}, height: {blockHeight} }} form {senderPubKey}, ignore.");
                return;
            }

            await BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs
            {
                SuggestedPeerPubKey = senderPubKey,
                BlockHash = blockHash.ToHex(),
                BlockHeight = blockHeight
            });
        }
    }
}