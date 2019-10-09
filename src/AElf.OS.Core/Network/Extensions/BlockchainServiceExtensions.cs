using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.OS.Network.Extensions
{
    public static class BlockchainServiceExtensions
    {
        public static async Task<BlockWithTransactions> GetBlockWithTransactionsByHash(this IBlockchainService blockchainService, Hash blockHash)
        {
            var block = await blockchainService.GetBlockByHashAsync(blockHash);

            if (block == null)
                return null;
            
            var transactions = await blockchainService.GetTransactionsAsync(block.TransactionIds);
            
            return new BlockWithTransactions { Header = block.Header, Transactions = { transactions }};
        }
        
        public static async Task<List<BlockWithTransactions>> GetBlocksWithTransactions(this IBlockchainService blockchainService,
            Hash firstHash, int count)
        {
            var blocks = await blockchainService.GetBlocksInBestChainBranchAsync(firstHash, count);
            
            var list = blocks
                .Select(async block =>
                {
                    var transactions = await blockchainService.GetTransactionsAsync(block.TransactionIds);
                    return new BlockWithTransactions { Header = block.Header, Transactions = { transactions } };
                });

            return (await Task.WhenAll(list)).ToList();
        }
    }
}