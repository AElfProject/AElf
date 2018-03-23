using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// Create a account from smrat contract code
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        Task<IAccount> CreateAccountAsync(SmartContractRegistration registration, IChain chain);

        IAccount GetAccountByHash(IHash hash);
    }
}