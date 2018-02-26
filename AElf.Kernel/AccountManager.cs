using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        private WorldState _worldState;
        private AccountZero _accountZero;
        private long _accountId;
        private static object _obj = "lock";

        public AccountManager(WorldState worldState, AccountZero accountZero)
        {
            _worldState = worldState;
            _accountZero = accountZero;
        }

        public Task ExecuteTransactionAsync(IAccount fromAccount, IAccount toAccount, ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Create normal Account without contract
        /// </summary>
        /// <returns></returns>
        public IAccount CreateAccount()
        {
            Hash<IAccount> hash;
            lock (_obj)
            {
                hash = new Hash<IAccount>(_accountZero.CalculateHashWith(_accountId++));
            }
            var account = new Account(hash);
            _worldState.AddAccountDataProvider(account);
            return account;
        }
        
        public Task<IAccount> CreateAccount(byte[] smartContract)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///  Create account with smartContractRegistration
        /// </summary>
        /// <param name="accountCaller"></param>
        /// <param name="contractName"></param>
        public async Task<IAccount> CreateAccount(IAccount accountCaller, string contractName)
        {
            const string smartContractMapKey = "SmartContractMap";

            // get the contract regiseter from dataProvider
            var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(_accountZero);
            var smartContractMap = accountZeroDataProvider.GetDataProvider()
                .GetDataProvider(smartContractMapKey);
            var smartContractRegistration = (SmartContractRegistration)
                smartContractMap
                    .GetAsync(new Hash<SmartContractRegistration>(_accountZero.CalculateHashWith(contractName))).Result;

            // inititalize the account and accountDataprovider
            var hash = new Hash<IAccount>(accountCaller.CalculateHashWith(_accountId++));
            var account = new Account(hash);
            _worldState.AddAccountDataProvider(account);
            var accountDataProvider = _worldState.GetAccountDataProviderByAccount(account);
            accountDataProvider.GetDataProvider().SetDataProvider(smartContractMapKey, new DataProvider(account, _worldState));
            
            // register smartcontract to the new contract
            var smartContractZero = new SmartContractZero();
            await smartContractZero.InititalizeAsync(accountDataProvider);
            await smartContractZero.RegisterSmartContract(smartContractRegistration);
            return account;
        }
    }
}
