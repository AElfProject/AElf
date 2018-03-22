using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace AElf.Kernel.KernelAccount
{

    
    
    public class SmartContractZero: ISmartContractZero
    {
        
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _cacheSmartContracts=new Dictionary<IHash, ISmartContract>();
        
        //private readonly IDictionary<IHash, IHash> _registeredContracts= new Dictionary<IHash, IHash>();

        

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private IChain _chain;

        private IWorldStateManager _worldStateManager;

        private IAccountManager _accountManager;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory, IChain chain,
            IWorldStateManager worldStateManager, IAccountManager accountManager)
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
            //_registeredContracts[hash] = reg.Hash;
        }

        public async Task<ISmartContract> GetSmartContractAsync(IHash hash)
        {
            if (_cacheSmartContracts.ContainsKey(hash))
                return _cacheSmartContracts[hash];
            
            // get SmartContractRegistration
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            //var regHash = _registeredContracts[hash];
            var obj = await smartContractMap.GetAsync(hash);
            
            // create smartcontract
            var reg = new SmartContractRegistration(obj);
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            var smartContract = await runner.RunAsync(reg);
            
            // init smartContract 
            var acc = _accountManager.GetAccountByHash(new Hash<IAccount>(hash.Value));
            var adp = _worldStateManager.GetAccountDataProvider(_chain, acc);
            await smartContract.InititalizeAsync(adp);
            
            // cache
            _cacheSmartContracts[hash] = smartContract;
            
            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;

        }

        public IHash<ISmartContract> GetHash()
        {
            return Hash<ISmartContract>.Zero;
        }
        
        /// <summary>
        /// deploy smartcontract
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="catagory"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Deploy(IHash<IAccount> caller, int catagory, byte[] data)
        {
            // caller account data context
            var callerAccount = _accountManager.GetAccountByHash(caller);
            var adp = _worldStateManager.GetAccountDataProvider(_chain, callerAccount);
            var dataContext = adp.Context;
            
            // create registration
            var smartContractRegistration = new SmartContractRegistration
            {
                Category = catagory,
                Bytes = data,
                Hash = new Hash<SmartContract>(dataContext.CalculateHashWith(data)) // temporary calculating for sm address
            };
            
            dataContext.IncreasementId++;
            
            // create new account for this contract
            var acc = await _accountManager.CreateAccountAsync(smartContractRegistration, _chain);
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
}