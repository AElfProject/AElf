using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public interface IBlockBody
    {
        IList<IHash> GetTransactions();

        bool AddTransaction(IHash tx);
    }
}