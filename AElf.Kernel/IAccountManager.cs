using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// execute a transaction from an account
        /// </summary>
        /// <param name="fromAccount"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task ExecuteTransactionAsync(IAccount fromAccount, ITransaction tx);
    }
}