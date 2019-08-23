using System.Linq;
using AElf.Kernel;

namespace AElf.OS.Network.Extensions
{
    public static class BlockWithTransactionsExtensions
    {
        public static Block ToBlock(this BlockWithTransactions block)
        {
            return new Block
            {
                Header = block.Header,
                Body = new BlockBody
                {
                    BlockHeader = block.Header.GetHash(),
                    TransactionIds = {block.Transactions.Select(tx => tx.GetHash()).ToList()}
                }
            };
        }
    }
}