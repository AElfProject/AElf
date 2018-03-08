using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.CodeDom.Compiler;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.KernelAccount
{

    public class SmartContractZero: ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;

        //_smartContracts should be implemented as a memory cache with expired.
        private readonly IDictionary<IHash, ISmartContract> _smartContracts = new Dictionary<IHash, ISmartContract>();

        

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private readonly IChain _chain;

        private readonly IWorldStateManager _worldStateManager;

        private readonly IAccountManager _accountManager;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory, IChain chain, IWorldStateManager worldStateManager, IAccountManager accountManager)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _chain = chain;
            _worldStateManager = worldStateManager;
            _accountManager = accountManager;
        }

        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash<IAccount> caller, string methodname, params object[] objs)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);
            
            await (Task) member.Invoke(this, objs);
        }
        
        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            await smartContractMap.SetAsync(reg.Hash, reg);
        }

        /// <summary>
        /// get sm associated with one account
        /// if not catched, constract the sm and add it in catch
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public async Task<ISmartContract> GetSmartContractAsync(IHash hash)
        {
            // return smartcontract if already exists in _smartContracts
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            
            // get smartcontractregistration from dataprovider
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            var obj = await smartContractMap.GetAsync(hash);
            var reg=new SmartContractRegistration(obj);

            // create smartcontract with registration
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            var smartContract = await runner.RunAsync(reg);

            // initialize smartcontract 
            var acc = _accountManager.GetAccountByHash(new Hash<IAccount>(hash.Value));
            var dp = _worldStateManager.GetAccountDataProvider(_chain, acc);
            await smartContract.InititalizeAsync(dp);

            // add sm to cache
            _smartContracts[hash] = smartContract;
            
            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;

        }

        
        public async Task Deploy(IHash<IAccount> caller, string contractName, int catagory, byte[] data)
        {
            // create registration
            var smartContractRegistration = new SmartContractRegistration
            {
                Name =  contractName,
                Category = catagory,
                Bytes = data,
                // temporary calculating method for new address
                Hash = new Hash<IAccount>(caller.CalculateHashWith(contractName))
            };

            // register to smartContractZero
            await RegisterSmartContract(smartContractRegistration);
        }
        
    }

    public interface ISmartContractRunnerFactory
    {
        ISmartContractRunner GetRunner(int category);
    }

    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg);
    }

    
    
    
    public class SmartContractRunner : ISmartContractRunner
    {
        public Task<ISmartContract> RunAsync(SmartContractRegistration reg)
        {
            
            /*
             * Contract written from user needs implement ISmartContract
             * if not, the contract cannot be invoked
             */
            
            // load assembly
            var data = reg.Bytes;
            var assembly = Assembly.Load(data);
            var type = assembly.GetType(reg.Name);
            
            // create instance
            var instance = (ISmartContract)assembly.CreateInstance(type.FullName);
            return Task.FromResult(instance);
        }
    }


    
    
    
}