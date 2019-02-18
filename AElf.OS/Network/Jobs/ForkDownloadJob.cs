using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.OS.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Jobs
{
    public class ForkDownloadJob : AsyncBackgroundJob<ForkDownloadJobArgs>
    {
        public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }
        
        public IFullBlockchainService BlockchainService { get; set; }
        public INetworkService NetworkService { get; set; }

        private int ChainId
        {
            get { return ChainOptions.Value.ChainId.ConvertBase58ToChainId(); }
        }
        
        public ForkDownloadJob()
        {
            Logger = NullLogger<ForkDownloadJob>.Instance;
        }

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            try
            {
                Logger?.LogDebug($"Starting download of {args.BlockHashes.Count} blocks from {args.Peer}.");
                
                var chain = await BlockchainService.GetChainAsync(ChainId);

                if (chain == null)
                {
                    Logger.LogError($"Failed to finish download of {args.BlockHashes.Count} blocks from {args.Peer}: chain not found.");
                }
            
                foreach (var hash in args.BlockHashes)
                {
                    // Check that some other job didn't get this before.
                    var hasBlock = await BlockchainService.HasBlockAsync(ChainId, hash);
                
                    if (hasBlock)
                        continue; // todo review maybe no need to go further.

                    // Query the peer
                    Block block = (Block)await NetworkService.GetBlockByHash(hash, args.Peer);

                    // Add to our chain
                    await BlockchainService.AddBlockAsync(ChainId, block);
                    await BlockchainService.AttachBlockToChainAsync(chain, block);
                    
                    Logger.LogDebug($"Added {block}.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job from {args.Peer}");
                throw;
            }
        }
    }
}