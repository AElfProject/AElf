using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IAccountManager
    {
        /// <summary>
        /// execute a transaction from an account
        /// </summary>
        /// <param name="fromAccount">caller account</param>
        /// <param name="toAccount">instance account</param>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task ExecuteTransactionAsync(IAccount fromAccount,IAccount toAccount, ITransaction tx);

        /// <summary>
        /// Create a account from smrat contract code
        /// </summary>
        /// <param name="smartContract"></param>
        /// <returns></returns>
        Task<IAccount> CreateAccount(byte[] smartContract);

        /// <summary>
        ///  Create account with smartContract in accountZero
        /// </summary>
        /// <param name="accountDataProvider"></param>
        /// <param name="contractName"></param>
        Task DeploySmartContractToAccount(IAccountDataProvider accountDataProvider, string contractName);
    }
}