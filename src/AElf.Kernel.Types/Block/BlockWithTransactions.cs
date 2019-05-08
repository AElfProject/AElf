using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace AElf.Kernel
{
    public partial class BlockWithTransactions : IBlock, IBlockWithTransactionBase
    {
        public IEnumerable<Transaction> FullTransactionList => Transactions;
        public IEnumerable<Hash> TransactionList => Transactions.Select(tx => tx.GetHash());
        
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $"{{ id: {GetHash()}, height: {Height} }}";
        }

        public BlockHeader Header
        {
            get { return BlockHeader; }
            set { BlockHeader = value; }
        }

        public BlockBody Body
        {
            get
            {
                var body = new BlockBody();
                body.Transactions.AddRange(Transactions.Select(tx => tx.GetHash()).ToList());
                return body;
            }
            set { throw new NotImplementedException(); }
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
        
        public byte[] GetHashBytes()
        {
            return Header.GetHashBytes();
        }

        Block IBlock.Clone()
        {
            throw new NotImplementedException();
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