using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero: ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _smartContracts=new Dictionary<IHash, ISmartContract>();

        

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private IChain _chain;

        private IWorldStateManager _worldStateManager;

        private IAccountManager _accountManager;

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

        public async Task<ISmartContract> GetSmartContractAsync(IHash hash)
        {
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            var obj = await smartContractMap.GetAsync(hash);
            
            var reg=new SmartContractRegistration(obj);

            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);

            var smartContract = await runner.RunAsync(reg);

            var acc = _accountManager.GetAccountByHash(new Hash<IAccount>(reg.Hash.Value));

            var dp = _worldStateManager.GetAccountDataProvider(_chain, acc);

            await smartContract.InititalizeAsync(dp);

            _smartContracts[hash] = smartContract;
            
            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;

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