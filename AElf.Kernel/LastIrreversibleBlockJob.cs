using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel
{
    public class LastIrreversibleBlockJob : AsyncBackgroundJob<LastIrreversibleBlockJobArgs>
    {
        public IBlockchainService BlockchainService { get; set; }

        protected override async Task ExecuteAsync(LastIrreversibleBlockJobArgs args)
        {
            Logger.LogDebug($"Start setting LIB job at height {args.BlockHeight}");

            var chain = await BlockchainService.GetChainAsync();
            var blockHash =
                await BlockchainService.GetBlockHashByHeightAsync(chain, args.BlockHeight, chain.BestChainHash);
            await BlockchainService.SetIrreversibleBlockAsync(chain, args.BlockHeight, blockHash);

            Logger.LogDebug($"Finish setting LIB job at height {args.BlockHeight}, block hash {blockHash}");
        }
    }
}