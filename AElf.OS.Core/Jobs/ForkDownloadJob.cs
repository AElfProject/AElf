using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

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
            Logger.LogDebug($"Enter ForkDownloadJob ...");
            try
            {
                var count = NetworkOptions.Value.BlockIdRequestCount;

                while (true)
                {
                    var chain = await BlockchainService.GetChainAsync();

                    var blockHash = chain.BestChainHash;

                    var blocks = await NetworkService.GetBlocksAsync(blockHash, count, args.SuggestedPeerAddress);
                    Logger.LogDebug($"Get {blocks.Count} blocks from {args.SuggestedPeerAddress}, request blockHash {blockHash.ToHex()}, count {count} ");

                    foreach (var block in blocks)
                    {
                        chain = await BlockchainService.GetChainAsync();
                        await BlockchainService.AddBlockAsync(block);
                        var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                        Logger.LogDebug($"AttachBlockToChainAsync Get status {status} for {block.BlockHashToHex}");
                        await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                    }

                    if (chain.LongestChainHeight >= args.BlockHeight || blocks.Count == 0)
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job");
            }
        }
    }
}