using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
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

                var hasBlock = await BlockchainService.HasBlockAsync(ChainId, blockHash);

                // if we have the block, nothing to do.
                if (hasBlock)
                {
                    if (await CheckAndFixLink(blockHash))
                        return;
                    
                    Logger.LogDebug($"Block {blockHash} already know.");
                    return;
                }

                var hasPrevious = await BlockchainService.HasBlockAsync(ChainId, header.PreviousBlockHash);

                // we have previous, so we only have one block to get.
                if (hasPrevious)
                {
                    Logger.LogWarning(
                        $"Previous block found {{ hash: {header.PreviousBlockHash}, height: {header.Height} }}.");

                    Block block = (Block) await NetworkService.GetBlockByHashAsync(blockHash, peer);

                    if (block == null)
                    {
                        Logger.LogWarning($"No peer has the block {{ hash: {blockHash}, height: {header.Height} }}.");
                        return;
                    }

                    await BlockchainService.AddBlockAsync(ChainId, block);

                    var chain = await BlockchainService.GetChainAsync(ChainId);
                    var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                    var link = await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

                    Logger.LogDebug($"Block processed {{ hash: {blockHash}, height: {header.Height} }}.");
                }
                else
                {
                    // If not we download block ids backwards until we link
                    // and queue the chain download as a background job.

                    List<Hash> idsToDownload = new List<Hash> {blockHash};

                    Hash topHash = blockHash;

                    for (ulong i = header.Height - 1; i > 1; i -= (ulong) BlockIdRequestCount)
                    {
                        Logger.LogTrace(
                            $"Query section: {{ top: {topHash}, count: {BlockIdRequestCount}, peer: {peer} }}");

                        // Ask the peer for the ids of the blocks
                        List<Hash> ids = await NetworkService
                            .GetBlockIdsAsync(topHash, BlockIdRequestCount,
                                peer); // todo this has to be in order, maybe add Height

                        if (ids == null || ids.Count == 0)
                        {
                            Logger.LogWarning($"Peer {peer} did not return any ids.");
                            break;
                        }

                        // Find the ids that we're missing
                        var unlinkableIds = await FindUnlinkableBlocksAsync(ids);

                        Logger.LogDebug($"Out of {ids.Count}, {unlinkableIds.Count} are missing.");

                        idsToDownload.AddRange(ids);

                        // If one or more blocks are linked
                        if (unlinkableIds.Count != ids.Count)
                            break;

                        topHash = idsToDownload.Last();
                    }

                    if (idsToDownload.Any())
                    {
                        Logger.LogDebug(
                            $"Queuing job to download {idsToDownload.Count} blocks: [{idsToDownload.First()}, ..., {idsToDownload.Last()}]");

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

        /// <summary>
        /// Returns true if it had to fix.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private async Task<bool> CheckAndFixLink(Hash hash)
        {
            try
            {
                var link = await BlockchainService.GetBlockByHeight(ChainId, hash);

                // If the block is already linked => no problem.
                if (link.IsLinked)
                    return false;
  
                var chain = await BlockchainService.GetChainAsync(ChainId);

                if (!chain.NotLinkedBlocks.Values.Contains(hash.ToStorageKey()))
                {
                    Logger.LogWarning($"Fixing link ({hash})");
                    // If the blocks that are not linked doesn't contain the unlinked block found in the 
                    // db, we try and re-inject it in the pipeline.
                    var block = await BlockchainService.GetBlockByHashAsync(ChainId, hash);
                    var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                    await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                    
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while checking and linking block.");
                return true;
            }

            return false;
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