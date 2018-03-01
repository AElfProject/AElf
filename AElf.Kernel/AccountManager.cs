using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        private IWorldState _worldState;

        public AccountManager(IWorldState worldState)
        {
            _worldState = worldState;
        }

        public Task ExecuteTransactionAsync(IAccount fromAccount, IAccount toAccount, ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> CreateAccount(byte[] smartContract)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///  Create account with smartContractContractRegistration
        /// </summary>
        /// <param name="accountCaller"></param>
        /// <param name="smartContractContractRegistration"></param>
        public async Task<IAccount> CreateAccount(IAccount accountCaller, SmartContractRegistration smartContractContractRegistration)
        {
            // inittitalize the account and accountDataprovider
            var hash = new Hash<IAccount>(accountCaller.CalculateHashWith(smartContractContractRegistration));
            var account = new Account(hash);
            var accountDataProvider = _worldState.GetAccountDataProviderByAccount(account);
            accountDataProvider.GetDataProvider().SetDataProvider("SmartContractMap");
            // register smartcontract to the new contract
            SmartContractZero smartContractZero = new SmartContractZero();
            await smartContractZero.InititalizeAsync(accountDataProvider);
            await smartContractZero.RegisterSmartContract(smartContractContractRegistration);
            return account;
        }
    }
}
