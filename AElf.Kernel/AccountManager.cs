using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        public Task ExecuteTransactionAsync(IAccount fromAccount, IAccount toAccount, ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> CreateAccount(byte[] smartContract)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> CreateAccount(IAccount accountCaller, SmartContractRegistration smartContractContractRegistration)
        {
            throw new System.NotImplementedException();
        }
    }
}