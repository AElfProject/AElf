using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public static class BlockchainServiceExtensions
    {
        public static async Task<Dictionary<long, Hash>> GetBlockIndexesAsync(this IBlockchainService blockchainService,
            long firstHeight, Hash bestChainHash)
        {
            var result = new Dictionary<long, Hash>();

            if (firstHeight == 0)
                return result;

            var chain = await blockchainService.GetChainAsync();
            result.Add(chain.BestChainHeight, chain.BestChainHash);

            var indexCount = chain.BestChainHeight - firstHeight - 1;
            var blockIndexes = await blockchainService.GetReversedBlockIndexes(chain.BestChainHash, (int) indexCount);

            foreach (var blockIndex in blockIndexes)
            {
                result.Add(blockIndex.Height, blockIndex.Hash);
            }

            return result;
        }
    }
}