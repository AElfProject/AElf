using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

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
            Hash firstHash, int count, ILogger logger = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var blocks = await blockchainService.GetBlocksInBestChainBranchAsync(firstHash, count);
            sw.Stop();
            
            logger?.LogDebug($"[Timing] from {firstHash} got {blocks.Count} blocks in {sw.Elapsed.TotalMilliseconds} ms");
            
            Stopwatch totalGetTxsTimer = Stopwatch.StartNew();
            var list = blocks
                .Select(async block =>
                {
                    Stopwatch getTxTimer = Stopwatch.StartNew();
                    var transactions = await blockchainService.GetTransactionsAsync(block.TransactionIds);
                    getTxTimer.Stop();
                    
                    if (getTxTimer.Elapsed.TotalMilliseconds > 50)
                        logger.LogDebug($"[Timing] from {firstHash} got txs for {block.GetHash()} in {getTxTimer.Elapsed.TotalMilliseconds} ms");

                    return new BlockWithTransactions { Header = block.Header, Transactions = { transactions } };
                });

            var fullBlocks = (await Task.WhenAll(list)).ToList();
            totalGetTxsTimer.Stop();
            
            logger.LogDebug($"[Timing] from {firstHash} completed get blocks in {totalGetTxsTimer.Elapsed.TotalMilliseconds} ms");

            return fullBlocks;
        }
    }
}