using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero : ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";

        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _smartContracts =
            new Dictionary<IHash, ISmartContract>();

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private IWorldStateManager _worldStateManager;

        private readonly IAccountContextService _accountContextService;
        
        private ISerializer<SmartContractRegistration> _serializer;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory,
            IWorldStateManager worldStateManager, ISerializer<SmartContractRegistration> serializer, 
            IAccountContextService accountContextService)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
            _serializer = serializer;
            _accountContextService = accountContextService;
        }

        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash caller, string methodname, params object[] objs)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);

            await (Task) member.Invoke(this, objs);
        }

        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            await smartContractMap.SetAsync(
                reg.Hash, _serializer.Serialize(reg)
            );
        }

        public async Task<ISmartContract> GetSmartContractAsync(Hash hash)
        {
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            
            // get SmartContractRegistration
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            //var regHash = _registeredContracts[hash];
            var obj = await smartContractMap.GetAsync(hash);
            var reg = _serializer.Deserialize(obj);

            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            var smartContract = await runner.RunAsync(reg);

            var dp = _worldStateManager.GetAccountDataProvider(_accountDataProvider.Context.ChainId, reg.Hash);

            await smartContract.InititalizeAsync(dp);

            _smartContracts[hash] = smartContract;

            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;
        }

        public IHash GetHash()
        {
            return Hash.Zero;
        }

        /// <summary>
        /// deploy a contract account
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="smartContractRegistration"></param>
        /// <returns></returns>
        public Task<Hash> DeployAccount(Hash caller, SmartContractRegistration smartContractRegistration)
        {
            // create new account for the contract
            var calllerContext =
                _accountContextService.GetAccountDataContext(caller, _accountDataProvider.Context.ChainId);
            
            var hash = new Hash(calllerContext.CalculateHashWith(smartContractRegistration.Bytes));
            _accountContextService.GetAccountDataContext(hash, _accountDataProvider.Context.ChainId);
            return Task.FromResult(hash);
        }
    }

    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg);
    }
}