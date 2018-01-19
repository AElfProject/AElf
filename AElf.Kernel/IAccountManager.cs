using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// Execute a transaction
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        List<Account> ExecuteTransactionAsync(ITransaction tx);
    }
}