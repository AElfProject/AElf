using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        private readonly WorldState _worldState;
        private readonly AccountZero _accountZero;
        private const string SmartContractMapKey = "SmartContractMap";
        public AccountManager(WorldState worldState, AccountZero accounZero)
        {
            _worldState = worldState;
            _accountZero = accounZero;
        }

        public Task ExecuteTransactionAsync(IAccount fromAccount, IAccount toAccount, ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> CreateAccount(byte[] smartContract)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// create accoutDataprovider for account
        /// </summary>
        /// <returns></returns>
        private Task<IAccountDataProvider> InitializeAccount(IAccountDataProvider accountDataProvider)
        {
            accountDataProvider.GetDataProvider()
                .SetDataProvider(SmartContractMapKey,
                    new DataProvider(_worldState, accountDataProvider.GetAccountAddress()));
                
            return Task.FromResult(accountDataProvider);
        }

        /// <summary>
        ///  deploy a contract to account
        /// </summary>
        /// <param name="accountDataProvider"></param>
        /// <param name="contractName"></param>
        public async Task DeploySmartContractToAccount(IAccountDataProvider accountDataProvider, string contractName)
        {
            // initialize the account and accountDataprovider
            await InitializeAccount(accountDataProvider);

            // get smartContractRegistration from accountZeroDataProvider
            var smartContractMap = _worldState.GetAccountDataProviderByAccount(_accountZero).GetDataProvider()
                .GetDataProvider(SmartContractMapKey);
            var smartContractRegistration = (SmartContractRegistration)
                smartContractMap
                    .GetAsync(new Hash<SmartContractRegistration>(Hash<IAccount>.Zero.CalculateHashWith(contractName)))
                    .Result;
            
            // register smartcontract to the new account
            var smartContractZero = new SmartContractZero();
            await smartContractZero.InititalizeAsync(accountDataProvider);
            await smartContractZero.RegisterSmartContract(smartContractRegistration);
        }
        
        
        
    }
}
