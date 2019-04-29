using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public static class LocalLibExtension
    {
        public static async Task<Block> GetIrreversibleBlockByHeightAsync(this IBlockchainService blockchainService,  long height)
        {
            var chain = await blockchainService.GetChainAsync();
            if (chain.LastIrreversibleBlockHeight < height)
                return null;
            var blockHash = await blockchainService.GetBlockHashByHeightAsync(chain, height, chain.BestChainHash);
            return await blockchainService.GetBlockByHashAsync(blockHash);
        }

        public static async Task<LastIrreversibleBlockDto> GetLibHashAndHeight(this IBlockchainService blockchainService)
        {
            var chain = await blockchainService.GetChainAsync();
            return new LastIrreversibleBlockDto
            {
                BlockHeight = chain.LastIrreversibleBlockHeight,
                BlockHash = chain.LastIrreversibleBlockHash
            };
        }
    }

    public class LastIrreversibleBlockDto
    {
        public Hash BlockHash { get; internal set;}
        public long BlockHeight { get; internal set;}
    }
}