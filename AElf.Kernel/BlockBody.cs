using System.Linq;

namespace AElf.Kernel
{
    public class BlockBody : IBlockBody
    {
        public IQueryable<ITransaction> GetTransactions()
        {
            throw new System.NotImplementedException();
        }
    }
}