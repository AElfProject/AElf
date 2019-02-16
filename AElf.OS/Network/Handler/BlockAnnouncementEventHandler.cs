using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.OS.Jobs;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Network.Handler
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>, ILocalEventHandler<AnnoucementReceivedEventData>, ISingletonDependency
    {
        public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }
        
        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public INetworkService NetworkService { get; set; }
        public IFullBlockchainService BlockchainService { get; set; }
        
        public ILogger<PeerConnectedEventHandler> Logger { get; set; }
        
        private int ChainId
        {
            get { return ChainOptions.Value.ChainId.ConvertBase58ToChainId(); }
        }
        
        public async Task HandleEventAsync(AnnoucementReceivedEventData eventData)
        {
            await ProcessNewBlock(eventData.Header, eventData.Peer);
        }
            
        public async Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            await ProcessNewBlock(eventData.Header, eventData.Peer);
        }

        private async Task ProcessNewBlock(BlockHeader header, string peer)
        {
            try
            {
                // todo protect this logic with LIB
                var blockHash = header.GetHash();
                var hasBlock = await BlockchainService.HasBlockAsync(ChainId, blockHash);

                // if we have the block, nothing to do.
                if (hasBlock)
                {
                    Logger?.LogDebug($"Block {blockHash} already know.");
                    return;
                }

                var hasPrevious = await BlockchainService.HasBlockAsync(ChainId, header.PreviousBlockHash);
                
                // we have previous, so we only have one block to get.
                if (hasPrevious)
                {
                    Block block = (Block) await NetworkService.GetBlockByHash(blockHash, peer);
                    await BlockchainService.AddBlockAsync(ChainId, block);
                }
                else
                {
                    // If not we download block ids backwards until we link
                    // and queue the chain download as a background job.
                    
                    List<Hash> idsToDownload = new List<Hash>();
                    
                    Hash topHash = blockHash;
            
                    for (ulong i = 0; i < header.Height; i -= NetworkConsts.DefaultBlockIdRequestCount)
                    {
                        List<Hash> ids = await NetworkService
                            .GetBlockIds(topHash, NetworkConsts.DefaultBlockIdRequestCount, peer); // todo this has to be in order

                        var unlinkableIds = await FindUnlinkableBlocksAsync(ids);

                        // if no more ids to get break the loop 
                        if (unlinkableIds.Count <= 0)
                            break;

                        idsToDownload.AddRange(ids);
                        topHash = idsToDownload.Last();
                    }
                    
                    if (idsToDownload.Any())
                        await BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs());
                }
            }
            catch (Exception e)
            {
                // todo 
                ;
            }
        }

        private async Task<List<Hash>> FindUnlinkableBlocksAsync(List<Hash> ids)
        {
            List<Hash> unlinkableIds = new List<Hash>();
            
            foreach (var id in ids)
            {
                if (await BlockchainService.HasBlockAsync(ChainId, id))
                {
                    // we have linked the fork
                    break;
                }
                            
                unlinkableIds.Add(id);           
            }

            return unlinkableIds;
        }
    }
}