using System.Linq;

namespace AElf.Kernel
{
    public static class BlockWithTransactionsExtensions
    {
        public static Block ToBlock(this BlockWithTransactions block) // todo move to OS
        {
            return new Block
            {
                Header = block.Header,
                Body = new BlockBody
                {
                    BlockHeader = block.Header.GetHash(),
                    Transactions = {block.Transactions.Select(tx => tx.GetHash()).ToList()}
                }
            };
        }
    }
}