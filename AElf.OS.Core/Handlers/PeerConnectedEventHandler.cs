using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Events;
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
        public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }
        public IOptionsSnapshot<NetworkOptions> NetworkOptions { get; set; }

        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public INetworkService NetworkService { get; set; }
        public IBlockchainService BlockchainService { get; set; }

        public IBlockchainExecutingService BlockchainExecutingService { get; set; }

        public ILogger<PeerConnectedEventHandler> Logger { get; set; }

        public PeerConnectedEventHandler()
        {
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

        private int ChainId => ChainOptions.Value.ChainId;

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

        // todo eventually protect this logic with LIB
        private async Task ProcessNewBlock(BlockHeader header, string peer)
        {
            if (header == null)
            {
                Logger.LogWarning($"Cannot process null header");
                return;
            }


            try
            {
                var blockHash = header.GetHash();

                Logger.LogTrace($"Processing header {{ hash: {blockHash}, height: {header.Height} }} from {peer}.");


                var block = await BlockchainService.GetBlockByHashAsync(ChainId, blockHash);
                
                
                if (block==null)
                {
                    block = (Block)  await NetworkService.GetBlockByHashAsync(blockHash);
                    Logger.LogDebug($"Block {blockHash} already know.");
                    return;
                }

                var chain = await BlockchainService.GetChainAsync(ChainId);

                var status = await BlockchainService.AttachBlockToChainAsync(chain, block);

                await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

                if (status.HasFlag(BlockAttachOperationStatus.NewBlockNotLinked))
                {
                }

            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error during {nameof(ProcessNewBlock)}, peer: {peer}.");
            }
        }

    }
}