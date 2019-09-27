using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public static class BlockchainServiceExtensions
    {
        public static async Task<List<IBlockIndex>> GetBlockIndexesAsync(this IBlockchainService blockchainService,
            long firstHeight, Hash bestChainHash, long bestChainHeight)
        {
            if (firstHeight == 0)
                return new List<IBlockIndex>();
            
            var indexCount = bestChainHeight - firstHeight - 1;
            var blockIndexes = await blockchainService.GetReversedBlockIndexes(bestChainHash, (int) indexCount);

            blockIndexes.Add(new BlockIndex
            {
                Hash = bestChainHash, 
                Height = bestChainHeight
            });

            return blockIndexes;
        }
    }
}