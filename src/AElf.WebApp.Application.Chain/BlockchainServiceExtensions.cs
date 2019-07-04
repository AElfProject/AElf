using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using System.Threading.Tasks;

namespace AElf.WebApp.Application.Chain
{
    public static class BlockchainServiceExtensions
    {
        internal static async Task<Block> GetBlockAsync(this IBlockchainService blockchainService, Hash blockHash)
        {
            return await blockchainService.GetBlockByHashAsync(blockHash);
        }

        internal static async Task<Block> GetBlockAtHeightAsync(this IBlockchainService blockchainService, long height)
        {
            return await blockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }
    }
}