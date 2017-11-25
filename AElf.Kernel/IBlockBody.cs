using System.Linq;

namespace AElf.Kernel
{
    public interface IBlockBody
    {
        IQueryable<ITransaction> GetTransactions();
    }
}