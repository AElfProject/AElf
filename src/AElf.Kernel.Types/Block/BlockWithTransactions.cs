using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockWithTransactions : IBlock, IBlockWithTransactionBase, ICustomDiagnosticMessage
    {        
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $"{{ id: {GetHash()}, height: {Height} }}";
        }

        public IEnumerable<Transaction> FullTransactionList => Transactions;
        public IEnumerable<Hash> TransactionList => Transactions.Select(tx => tx.GetHash());
        public BlockBody Body => new BlockBody { Transactions = { Transactions.Select(tx => tx.GetHash()).ToList() }}; 
        public long Height => BlockHeader?.Height ?? 0;
        
        public BlockHeader Header
        {
            get { return BlockHeader; }
            set { BlockHeader = value; }
        }
        
        public Hash GetHash()
        {
            return BlockHeader.GetHash();
        }
        
        public byte[] GetHashBytes()
        {
            return Header.GetHashBytes();
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