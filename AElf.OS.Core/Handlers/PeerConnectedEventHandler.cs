using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
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
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>, ILocalEventHandler<AnnoucementReceivedEventData>
    {
        public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }

        public IBackgroundJobManager BackgroundJobManager { get; set; }
        public INetworkService NetworkService { get; set; }
        public IBlockchainService BlockchainService { get; set; }

         public ILogger<PeerConnectedEventHandler> Logger { get; set; }

         public PeerConnectedEventHandler()
        {
            Logger = NullLogger<PeerConnectedEventHandler>.Instance;
        }

         private int ChainId => ChainOptions.Value.ChainId;

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

                 var hasBlock = await BlockchainService.HasBlockAsync(ChainId, blockHash);

                 // if we have the block, nothing to do.
                if (hasBlock)
                {
                    Logger.LogDebug($"Block {blockHash} already know.");
                    return;
                }

                 var hasPrevious = await BlockchainService.HasBlockAsync(ChainId, header.PreviousBlockHash);

                 // we have previous, so we only have one block to get.
                if (hasPrevious)
                {
                    Logger.LogWarning($"Previous block found {{ hash: {header.PreviousBlockHash}, height: {header.Height} }}.");

                     Block block = (Block) await NetworkService.GetBlockByHashAsync(blockHash, peer);

                     if (block == null)
                    {
                        Logger.LogWarning($"No peer has the block {{ hash: {blockHash}, height: {header.Height} }}.");
                        return;
                    }

                     await BlockchainService.AddBlockAsync(ChainId, block);

                     var chain = await BlockchainService.GetChainAsync(ChainId);
                    var link = await BlockchainService.AttachBlockToChainAsync(chain, block);

                     Logger.LogDebug($"Block processed {{ hash: {blockHash}, height: {header.Height} }}.");
                }
                else
                {
                    // If not we download block ids backwards until we link
                    // and queue the chain download as a background job.

                     List<Hash> idsToDownload = new List<Hash>();

                     Hash topHash = blockHash;

                     for (ulong i = 0; i < header.Height; i -= NetworkConsts.DefaultBlockIdRequestCount)
                    {
                        // Ask the peer for the ids of the blocks
                        List<Hash> ids = await NetworkService
                            .GetBlockIdsAsync(topHash, NetworkConsts.DefaultBlockIdRequestCount, peer); // todo this has to be in order, maybe add Height

                         // Find the ids that we're missing
                        var unlinkableIds = await FindUnlinkableBlocksAsync(ids);

                         // If no more ids to get break the loop 
                        if (unlinkableIds.Count <= 0)
                            break;

                         idsToDownload.AddRange(ids);
                        topHash = idsToDownload.Last();
                    }

                    if (idsToDownload.Any())
                    {
                        await BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs
                        {
                            BlockHashes = idsToDownload.Select(b => b.DumpByteArray()).ToList(), 
                            Peer = peer
                        });
                    }
                    else
                    {
                        Logger.LogWarning($"No blocks where needed but previous was not found for " +
                                          $"{{ previous: {header.PreviousBlockHash} hash: {blockHash}, height: {header.Height} }}.");
                    }

                 }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error during {nameof(ProcessNewBlock)}, peer: {peer}.");
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