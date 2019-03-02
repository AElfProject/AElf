using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>,
        ILocalEventHandler<AnnoucementReceivedEventData>
    {
        public IOptionsSnapshot<NetworkOptions> NetworkOptions { get; set; }

        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public INetworkService NetworkService { get; set; }
        public IBlockchainService BlockchainService { get; set; }

        public IBlockchainExecutingService BlockchainExecutingService { get; set; }

        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        public IChainRelatedComponentManager<IAElfNetworkServer> NetworkServers { get; set; }

        public PeerConnectedEventHandler()
        {
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }


        private int BlockIdRequestCount =>
            NetworkOptions?.Value?.BlockIdRequestCount ?? NetworkConsts.DefaultBlockIdRequestCount;

        public async Task HandleEventAsync(AnnoucementReceivedEventData eventData)
        {
            await ProcessNewBlock(eventData.Header, eventData.Peer);
        }

        public async Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            await ProcessNewBlock(eventData.Header, eventData.Peer);
        }

        private async Task ProcessNewBlock(BlockHeader header, string peerAddress)
        {
            var blockHeight = header.Height;
            var blockHash = header.GetHash();
            var chainId = header.ChainId;


            var peerInPool = NetworkServers.Get(chainId).PeerPool.FindPeer(peerAddress);
            if (peerInPool != null)
            {
                peerInPool.CurrentBlockHash = blockHash;
                peerInPool.CurrentBlockHeight = blockHeight;
            }
            
            var chain = await BlockchainService.GetChainAsync(chainId);

            if (header.Height - chain.LongestChainHeight < 10)
            {
                //currently: 100
                //remote: 200
                //just ignore the block

                return;
            }

            try
            {
                Logger.LogTrace(
                    $"Processing header {{ hash: {blockHash}, height: {blockHeight} }} from {peerAddress}.");

                var block = await BlockchainService.GetBlockByHashAsync(chainId, blockHash);

                if (block == null)
                {
                    block = (Block) await NetworkService.GetBlockByHashAsync(blockHash);
                    Logger.LogDebug($"Block {blockHash} already know.");
                    return;
                }

                var status = await BlockchainService.AttachBlockToChainAsync(chain, block);

                await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

                if (status.HasFlag(BlockAttachOperationStatus.NewBlockNotLinked))
                {
                    await BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs	
                    {	
                        SuggestedPeerAddress = peerAddress,
                        ChainId = chainId,
                        
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error during {nameof(ProcessNewBlock)}, peer: {peerAddress}.");
            }
        }
    }
}