using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public static class BlockchainServiceExtensions
    {
        public static async Task<Dictionary<long, Hash>> GetBlockIndexesAsync(this IBlockchainService blockchainService,
            long firstHeight, Hash bestChainHash, long bestChainHeight)
        {
            var result = new Dictionary<long, Hash>();

            if (firstHeight == 0)
                return result;

            result.Add(bestChainHeight, bestChainHash);

            var indexCount = bestChainHeight - firstHeight - 1;
            var blockIndexes = await blockchainService.GetReversedBlockIndexes(bestChainHash, (int) indexCount);

            foreach (var blockIndex in blockIndexes)
            {
                result.Add(blockIndex.Height, blockIndex.Hash);
            }

            return result;
        }
    }
}