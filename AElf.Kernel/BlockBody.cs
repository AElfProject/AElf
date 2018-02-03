using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public class BlockBody : IBlockBody
    {
        private List<ITransaction> _transactions = new List<ITransaction>();

        public int TransactionsCount => _transactions.Count;

        public BlockBody() { }

        public IQueryable<ITransaction> GetTransactions() => _transactions.AsQueryable();

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