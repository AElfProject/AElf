using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.OS.Network;
using Microsoft.Extensions.Options;

namespace AElf.OS.Jobs
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

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            var chain = await BlockchainService.GetChainAsync(ChainId);
            
            foreach (var hash in args.BlockHashes)
            {
                var hasBlock = await BlockchainService.HasBlockAsync(ChainId, hash);
                
                if (hasBlock)
                    continue; // todo review

                Block block = (Block)await NetworkService.GetBlockByHash(hash, args.Peer);

                await BlockchainService.AddBlockAsync(ChainId, block);
                await BlockchainService.AttachBlockToChainAsync(chain, block);
            }
        }
    }
}