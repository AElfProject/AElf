using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class SmartContractManager: ISmartContractManager
    {
        public AccountManager AccountManager { get; }

        public SmartContractManager(AccountManager accountManager)
        {
            AccountManager = accountManager;
        }

        public async Task<ISmartContract> GetAsync(IAccount account)
        {
            // if new account, accountDataProvider will be automatically created and stored in worldstate 
            var accountDataProvider = AccountManager.WorldState.GetAccountDataProviderByAccount(account);
            
            return await CreateSmartContract(accountDataProvider);
        }

        private async Task<ISmartContract> CreateSmartContract(IAccountDataProvider accountDataProvider)
        {
            var smartContract = new SmartContract(this);
            await smartContract.InitializeAsync(accountDataProvider);
            return smartContract;
        }
        
        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            await AccountManager.AccountZero.SmartContractZero.RegisterSmartContract(reg);
        }
    }
}