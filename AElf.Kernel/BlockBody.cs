using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockBody : IBlockBody
    {
        private List<ITransaction> Transactions { get; set; }

        public int TransactionsCount
        {
            get
            {
                return Transactions.Count;
            }
        }

        public BlockBody() { }

        public IQueryable<ITransaction> GetTransactions()
        {
            return Transactions.AsQueryable();
        }

        public bool AddTransaction(ITransaction tx)
        {
            //Avoid duplication of addition.
            if (Transactions.Exists(t => t.GetHash() == tx.GetHash()))
            {
                return false;
            }
            Transactions.Add(tx);
            return true;
        }
    }
}