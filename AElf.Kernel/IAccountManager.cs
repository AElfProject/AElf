using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// Create a account from smrat contract code
        /// </summary>
        /// <param name="smartContract"></param>
        /// <returns></returns>
        Task<IAccount> CreateAccount(byte[] smartContract);

        IAccount GetAccountByHash(IHash<IAccount> hash);
    }
}