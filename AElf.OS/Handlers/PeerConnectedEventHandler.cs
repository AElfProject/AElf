using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Jobs;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>, ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public INetworkService NetworkService { get; set; }
        public IBlockchainService BlockchainService { get; set; }
        public IBlockchainExecutingService BlockchainExecutingService { get; set; }
        public IPeerPool PeerPool { get; set; }
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

            if (blockHeight - chain.LongestChainHeight > 5)
            {
                //currently: 100, remote: 200, just ignore the block
                Logger.LogTrace($"Received a higher chain {blockHeight}, current height {chain.LongestChainHeight}, start sync ...");
                await EnqueueDownloadJobAsync(senderPubKey, header.Announce.BlockHash.ToHex(), blockHeight);
                return;
            }

            if (blockHeight < chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {blockHash}, height: {blockHeight} }} form {senderPubKey}, ignore.");
                return;
            }

            var block = await BlockchainService.GetBlockByHashAsync(blockHash);
            if (block != null)
            {
                Logger.LogDebug($"Block {blockHash} already know.");
                return;
            }

            block = await NetworkService.GetBlockByHashAsync(blockHash);

            await BlockchainService.AddBlockAsync(block);

            var status = await BlockchainService.AttachBlockToChainAsync(chain, block);

            await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

            if (status.HasFlag(BlockAttachOperationStatus.NewBlockNotLinked))
            {
                await EnqueueDownloadJobAsync(senderPubKey, header.Announce.BlockHash.ToHex(), blockHeight);
            }
        }

        private async Task EnqueueDownloadJobAsync(string senderPubKey, string blockHash, long blockHeight)
        {
            await BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs
            {
                SuggestedPeerPubKey = senderPubKey,
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
}