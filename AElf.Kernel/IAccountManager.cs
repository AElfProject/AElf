using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// send a transaction from an account
        /// </summary>
        /// <param name="fromAccount"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task SendTransactionAsync(IAccount fromAccount, ITransaction tx);
    }
}