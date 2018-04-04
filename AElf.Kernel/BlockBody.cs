using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        private readonly List<IHash> _transactions = new List<IHash>();

        public int TransactionsCount => _transactions.Count;


        public bool AddTransaction(IHash tx)
        {
            
            if (_transactions.Contains(tx))
                return false;
            _transactions.Add(tx);
            return true;
        }
    }
}