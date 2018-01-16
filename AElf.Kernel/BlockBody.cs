using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockBody : IBlockBody
    {
        private List<ITransaction> _transactions { get; set; } = new List<ITransaction>();

        public int TransactionsCount
        {
            get
            {
                return _transactions.Count;
            }
        }

        public BlockBody() { }

        public IQueryable<ITransaction> GetTransactions()
        {
            return _transactions.AsQueryable();
        }

        public bool AddTransaction(ITransaction tx)
        {
            //Avoid duplication of addition.
            if (_transactions.Exists(t => t.GetHash() == tx.GetHash()))
            {
                return false;
            }
            _transactions.Add(tx);
            return true;
        }
    }
}