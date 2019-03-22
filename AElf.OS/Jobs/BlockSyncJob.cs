using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.OS.Jobs
{
    public class BlockSyncJob : AsyncBackgroundJob<BlockSyncJobArgs>
    {
        private const long InitialSyncLimit = 10;
        public IBlockchainService BlockchainService { get; set; }
        public IBlockchainExecutingService BlockchainExecutingService { get; set; }
        public INetworkService NetworkService { get; set; }
        public IOptionsSnapshot<NetworkOptions> NetworkOptions { get; set; }

        protected override async Task ExecuteAsync(BlockSyncJobArgs args)
        {
            Logger.LogDebug($"Start block sync job, target height: {args.BlockHeight}, target block hash: {args.BlockHash}, peer: {args.SuggestedPeerPubKey}");

            var chain = await BlockchainService.GetChainAsync();
            try
            {
                if (!args.BlockHash.IsNullOrEmpty() && args.BlockHeight - chain.LongestChainHeight < 5)
                {
                    var peerBlockHash = Hash.LoadHex(args.BlockHash);
                    var peerBlock = await BlockchainService.GetBlockByHashAsync(peerBlockHash);
                    if (peerBlock != null)
                    {
                        Logger.LogDebug($"Block {peerBlockHash} already know.");
                        return;
                    }

                    peerBlock = await NetworkService.GetBlockByHashAsync(peerBlockHash);
                    if (peerBlock == null)
                    {
                        Logger.LogWarning($"Get null block from peer, request block hash: {peerBlockHash}");
                        return;
                    }
                    var status = await AttachBlockToChain(peerBlock);
                    if (!status.HasFlag(BlockAttachOperationStatus.NewBlockNotLinked))
                    {
                        return;
                    }
                }

                var blockHash = chain.LastIrreversibleBlockHash;
                Logger.LogDebug($"Trigger sync blocks from peers, lib height: {chain.LastIrreversibleBlockHeight}, lib block hash: {blockHash}");

                var blockHeight = chain.LastIrreversibleBlockHeight;
                var count = NetworkOptions.Value.BlockIdRequestCount;
                var peerBestChainHeight = await NetworkService.GetBestChainHeightAsync();
                while (true)
                {
                    Logger.LogDebug($"Request blocks start with {blockHash}");
                    
                    var peer = peerBestChainHeight - blockHeight > InitialSyncLimit ? null : args.SuggestedPeerPubKey;
                    var blocks = await NetworkService.GetBlocksAsync(blockHash, blockHeight, count, peer);

                    if (blocks == null || !blocks.Any())
                    {
                        Logger.LogDebug($"No blocks returned, current chain height: {chain.LongestChainHeight}.");
                        break;
                    }

                    Logger.LogDebug($"Received [{blocks.First()},...,{blocks.Last()}] ({blocks.Count})");

                    if (blocks.First().Header.PreviousBlockHash != blockHash)
                    {
                        Logger.LogError($"Current job hash : {blockHash}");
                        throw new InvalidOperationException($"Previous block not match previous {blockHash}, network back {blocks.First().Header.PreviousBlockHash}");
                    }

                    foreach (var block in blocks)
                    {
                        if (block == null)
                        {
                            Logger.LogWarning($"Get null block from peer, request block start: {blockHash}");
                            break;
                        }
                        Logger.LogDebug($"Processing block {block},  longest chain hash: {chain.LongestChainHash}, best chain hash : {chain.BestChainHash}");
                        await AttachBlockToChain(block);
                    }

                    chain = await BlockchainService.GetChainAsync();
                    peerBestChainHeight = await NetworkService.GetBestChainHeightAsync();
                    if (chain.LongestChainHeight >= peerBestChainHeight)
                    {
                        break;
                    }

                    var lastBlock = blocks.Last();
                    blockHash = lastBlock.GetHash();
                    blockHeight = lastBlock.Height;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish block sync job");
            }
            finally
            {
                Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
            }
        }

        private async Task<BlockAttachOperationStatus> AttachBlockToChain(Block block)
        {
            var chain = await BlockchainService.GetChainAsync();
            await BlockchainService.AddBlockAsync(block);
            var status = await BlockchainService.AttachBlockToChainAsync(chain, block);                        
            await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
            return status;
        }
    }
}