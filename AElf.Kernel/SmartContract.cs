using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{ 
    public class SmartContract : ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;
        private readonly SmartContractManager _smartContractManager;

        public SmartContract(SmartContractManager smartContractManager)
        {
            _smartContractManager = smartContractManager;
        }

        public async Task InitializeAsync(IAccountDataProvider accountDataProvider)
        {
            _accountDataProvider = accountDataProvider;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Invoke smartcontract method
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="methodName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task InvokeAsync(IAccount caller, string methodName, params object[] param)
        {
            if (methodName == "Transfer" || methodName == "CreateAccount" || methodName == "DeploySmartContract")
            {
                var type = typeof(SmartContract);
                var member = type.GetMethod(methodName);
                var objs = new object[1 + param.Length];
                objs[0] = caller;
                param.CopyTo(objs, 1);
            
                await (Task) member.Invoke(this, objs);
                //await CreateAccount(caller, (string)param[0]);
            }
            else
            {
                // get smartContractRegistration by accountDataProvider 
                var smartContractRegistration = (SmartContractRegistration) _accountDataProvider.GetDataProvider()
                    .GetDataProvider("SmartContractMap")
                    .GetAsync(new Hash<SmartContractRegistration>(_accountDataProvider.CalculateHashWith(param[0])))
                    .Result;
            
                // load assembly with bytes
                Assembly assembly = Assembly.Load(smartContractRegistration.Bytes);
                var instance = assembly.CreateInstance(assembly.GetTypes().ElementAt(0).ToString());
                var method = instance.GetType().GetMethod(methodName);
                
                
                // if contract is static, first param will be ignore
                await (Task) method.Invoke(instance, param);
                
            }
            
        }
        


        /// <summary>
        /// deploy a new smartcontract with tx
        /// and accountTo is created associated with the new contract
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="contractName"></param>
        /// <param name="smartContractCode"></param>
        /// <param name="category"> 1: C# bytes </param>
        private async Task DeploySmartContract(IAccount caller, int category, string contractName, byte[] smartContractCode)
        {
            
            var smartContractRegistration = new SmartContractRegistration
            {
                Name = contractName,
                Bytes = smartContractCode,
                Category = category,
                Hash = new Hash<SmartContractRegistration>(Hash<IAccount>.Zero.CalculateHashWith(contractName))
            };
            
            // register contracts on accountZero
            await _smartContractManager.RegisterSmartContract(smartContractRegistration);

            // deploy smart contract
            await _smartContractManager.AccountManager.DeploySmartContract(caller, contractName);
        }


        /// <summary>
        /// Create a new account with old contract
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="contractName"></param>
        /// <returns></returns>
        public async Task CreateAccount(IAccount caller, string contractName)
        {
            // create new account with a contract already in accountZero
            await _smartContractManager.AccountManager.DeploySmartContract(caller, contractName);
        }
        
        
        /// <summary>
        /// transfer coins between accounts
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="param"></param>
        private async Task Transfer(IAccount caller, object[] param)
        {
            // get accountDataProviders from WorldState
            var accountFromDataProvider =
                _smartContractManager.AccountManager.WorldState.GetAccountDataProviderByAccount(caller);
            
            // use dataProvider to get Balance obj
            var fromBalanceHash = new Hash<double>(caller.GetAddress().CalculateHashWith("Balance"));
            var fromBalanceDataProvider = accountFromDataProvider.GetDataProvider().GetDataProvider("Balance");
            var fromBalance = fromBalanceDataProvider.GetAsync(fromBalanceHash).Result;

            var toBalanceHash =
                new Hash<double>(_accountDataProvider.GetAccountAddress().CalculateHashWith("Balance"));
            var toBalanceDataProvider = _accountDataProvider.GetDataProvider().GetDataProvider("Balance");
            var toBalance = toBalanceDataProvider.GetAsync(toBalanceHash).Result;

            // TODO: calculate with amount 
            

            // TODO: uodate dataProvider
            await fromBalanceDataProvider.SetAsync(fromBalanceHash, fromBalance);
            await toBalanceDataProvider.SetAsync(toBalanceHash, toBalance);
            
        }

        
    }
}