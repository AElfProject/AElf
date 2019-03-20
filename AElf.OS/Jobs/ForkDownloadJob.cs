using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJob : AsyncBackgroundJob<ForkDownloadJobArgs>
    {
        public IBlockchainService BlockchainService { get; set; }
        public IBlockchainExecutingService BlockchainExecutingService { get; set; }
        public INetworkService NetworkService { get; set; }
        public IOptionsSnapshot<NetworkOptions> NetworkOptions { get; set; }

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            Logger.LogDebug($"Fork job: {{ target: {args.BlockHeight}, peer: {args.SuggestedPeerPubKey} }}");

            try
            {
                var count = NetworkOptions.Value.BlockIdRequestCount;
                var chain = await BlockchainService.GetChainAsync();

                var blockHash = chain.LastIrreversibleBlockHash;

                while (true)
                {
                    Logger.LogDebug($"Request blocks start with {blockHash}");

                    var blocks = await NetworkService.GetBlocksAsync(blockHash, count, args.SuggestedPeerPubKey);

                    if (blocks == null || !blocks.Any())
                    {
                        Logger.LogDebug($"No blocks returned, block-count {{ chain height: {chain.LongestChainHeight} }}.");
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
                        chain = await BlockchainService.GetChainAsync();
                        Logger.LogDebug($"Processing {block}. Chain is {{ longest: {chain.LongestChainHash}, best: {chain.BestChainHash} }} ");

                        await BlockchainService.AddBlockAsync(block);
                        var status = await BlockchainService.AttachBlockToChainAsync(chain, block);                        
                        await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                    }

                    if (chain.LongestChainHeight >= await NetworkService.GetBestChainHeightAsync())
                    {
                        Logger.LogDebug($"Finishing job: {{ chain height: {chain.LongestChainHeight} }}");
                        break;
                    }

                    blockHash = blocks.Last().GetHash();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job");
            }
        }
    }
}