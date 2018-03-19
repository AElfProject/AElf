using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public class BlockBody : IBlockBody
    {
        private readonly List<IHash<ITransaction>> _transactions = new List<IHash<ITransaction>>();

        public int TransactionsCount => _transactions.Count;

        public BlockBody() { }

        public IList<IHash<ITransaction>> GetTransactions()
        {
            return _transactions;
        }

        public bool AddTransaction(IHash<ITransaction> tx)
        {
            if (_transactions.Contains(tx))
                return false;
            _transactions.Add(tx);
            return true;
        }
    }
}