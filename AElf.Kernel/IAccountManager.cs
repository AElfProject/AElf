using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// Create a account from smrat contract code
        /// </summary>
        /// <param name="smartContract"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        Task<IAccount> CreateAccountAsync(byte[] smartContract, IChain chain);

        IAccount GetAccountByHash(Hash hash);
    }
}