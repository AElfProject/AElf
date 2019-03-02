using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJob : AsyncBackgroundJob<ForkDownloadJobArgs>
    {
        public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }

        public IBlockchainService BlockchainService { get; set; }

        public IBlockchainExecutingService BlockchainExecutingService { get; set; }
        public INetworkService NetworkService { get; set; }

        private int ChainId => ChainOptions.Value.ChainId;

        public ForkDownloadJob()
        {
            Logger = NullLogger<ForkDownloadJob>.Instance;
        }

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            try
            {
                Logger.LogDebug($"Starting download of {args.BlockHashes.Count} blocks from {args.Peer}.");

                args.BlockHashes.Reverse();

                foreach (var hash in args.BlockHashes.Select(Hash.LoadByteArray))
                {
                    // Check that some other job didn't get this before.
                    var hasBlock = await BlockchainService.HasBlockAsync(ChainId, hash);

                    if (hasBlock)
                    {
                        Logger.LogDebug($"Block {hash} already know, skipping.");
                        continue;
                    }

                    // Query the peer
                    Block block = (Block) await NetworkService.GetBlockByHashAsync(hash, args.Peer);

                    if (block == null)
                    {
                        Logger.LogWarning($"Aborting download, could not get {hash} from {args.Peer}");
                        continue;
                    }

                    var chain = await BlockchainService.GetChainAsync(ChainId);

                    if (chain == null)
                    {
                        Logger.LogError($"Failed to finish download of {args.BlockHashes.Count} blocks from {args.Peer}: chain not found.");
                        break;
                    }
                    
                    // Add to our chain
                    await BlockchainService.AddBlockAsync(ChainId, block);
                    var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                    await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

                    Logger.LogDebug($"Added {block}.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job from {args.Peer}");
            }
        }
    }
}