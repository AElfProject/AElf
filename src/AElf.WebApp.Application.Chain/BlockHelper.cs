using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.WebApp.Application.Chain
{
    public static class BlockHelper
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