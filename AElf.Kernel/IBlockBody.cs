using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public interface IBlockBody
    {
        IList<IHash<ITransaction>> GetTransactions();

        bool AddTransaction(IHash<ITransaction> tx);
    }
}