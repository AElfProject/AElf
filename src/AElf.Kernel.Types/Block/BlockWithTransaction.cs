using System.Linq;
using System.Collections.Generic;

namespace AElf.Kernel
{
    public partial class BlockWithTransaction
    {
        public IEnumerable<Transaction> TransactionList => Transactions;
        
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $"{{ id: {GetHash()}, height: {Height} }}";
        }
        
        public long Height
        {
            get => BlockHeader?.Height ?? 0;
            set { }
        }
        
        public Hash GetHash()
        {
            return BlockHeader.GetHash();
        }
        
        public Block ToBlock()
        {
            return new Block
            {
                Header = BlockHeader,
                Body = new BlockBody
                {
                    BlockHeader = BlockHeader.GetHash(),
                    Transactions = {Transactions.Select(tx => tx.GetHash()).ToList()}
                }
            };
        }
    }
}