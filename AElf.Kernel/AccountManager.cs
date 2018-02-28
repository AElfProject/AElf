using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        public WorldState WorldState { get; }
        public AccountZero AccountZero { get; }
        private const string SmartContractMapKey = "SmartContractMap";

        public AccountManager(WorldState worldState, AccountZero accountZero)
        {
            WorldState = worldState;
            AccountZero = accountZero;
        }

        public Task ExecuteTransactionAsync(IAccount fromAccount, IAccount toAccount, ITransaction tx)
        {
            throw new System.NotImplementedException();
        }


       
        /// <summary>
        /// register the contract to accountZero
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
             await AccountZero.SmartContractZero.RegisterSmartContract(reg);
        }

        
        /// <summary>
        /// create accoutDataprovider for account
        /// </summary>
        /// <returns></returns>
        private Task<IAccountDataProvider> InitializeAccount(IAccountDataProvider accountDataProvider)
        {
            accountDataProvider.GetDataProvider()
                .SetDataProvider(SmartContractMapKey,
                    new DataProvider(WorldState, accountDataProvider.GetAccountAddress()));
                
            return Task.FromResult(accountDataProvider);
        }

        /// <summary>
        ///  deploy a contract to account
        /// </summary>
        /// <param name="accountDataProvider"></param>
        /// <param name="contractName"></param>
        public async Task DeploySmartContract(IAccountDataProvider accountDataProvider, string contractName)
        {
            // initialize the account and accountDataprovider
            await InitializeAccount(accountDataProvider);

            // get smartContractRegistration from accountZeroDataProvider
            var smartContractMap = WorldState.GetAccountDataProviderByAccount(AccountZero).GetDataProvider()
                .GetDataProvider(SmartContractMapKey);
            var smartContractRegistration = (SmartContractRegistration)
                smartContractMap
                    .GetAsync(new Hash<SmartContractRegistration>(Hash<IAccount>.Zero.CalculateHashWith(contractName)))
                    .Result;

            var reg = new SmartContractRegistration
            {
                Category = smartContractRegistration.Category,
                Name = smartContractRegistration.Name,
                Bytes = smartContractRegistration.Bytes,
                Hash = new Hash<SmartContractRegistration>(accountDataProvider.GetAccountAddress().CalculateHashWith(contractName))
            };
            
            // register smartcontract to the new account
            var smartContractZero = new SmartContractZero();
            
            await smartContractZero.InitializeAsync(accountDataProvider);
            await smartContractZero.RegisterSmartContract(reg);
        }
        
        
        
    }
}
