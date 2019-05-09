using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockWithTransactions : IBlock, IBlockWithTransactionBase, ICustomDiagnosticMessage
    {
        partial void OnConstruction()
        {
            Header = new BlockHeader();
        }

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
        public long Height => Header?.Height ?? 0;        
        
        public Hash GetHash()
        {
            return Header.GetHash();
        }
        
        public byte[] GetHashBytes()
        {
            return Header.GetHashBytes();
        }
    }
}