using System;
using System.Linq;
using System.Threading.Tasks;
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
            Logger.LogDebug($"Enter ForkDownloadJob ...");
            try
            {
                var count = NetworkOptions.Value.BlockIdRequestCount;

                var chain = await BlockchainService.GetChainAsync();
                var blockHash = chain.LongestChainHash;

                while (true)
                {
                    var blocks = await NetworkService.GetBlocksAsync(blockHash, count, args.SuggestedPeerAddress);
                    Logger.LogDebug($"Get {blocks.Count} blocks for request {args}");

                    foreach (var block in blocks)
                    {
                        chain = await BlockchainService.GetChainAsync();
                        await BlockchainService.AddBlockAsync(block);
                        var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                        await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                    }

                    if (chain.LongestChainHeight >= args.BlockHeight || blocks.Count == 0)
                        break;

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