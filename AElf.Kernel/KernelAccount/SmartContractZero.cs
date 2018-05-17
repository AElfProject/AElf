using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ServiceStack;
using Type = Google.Protobuf.WellKnownTypes.Type;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero : ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";

        private const string SMART_CONTRACT_INSTANCES = "SmartContractInstances";

        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _smartContracts =
            new Dictionary<IHash, ISmartContract>();

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private readonly IWorldStateManager _worldStateManager;

        private readonly IAccountContextService _accountContextService;
        

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory,
            IWorldStateManager worldStateManager, IAccountContextService accountContextService)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
            _accountContextService = accountContextService;
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(SmartContractInvokeContext context)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(context.MethodName);
            var p = member.GetParameters()[0]; //first parameters
            
            ProtobufSerializer serializer=new ProtobufSerializer();
            var obj = serializer.Deserialize(context.Params.ToByteArray(), p.ParameterType.DeclaringType);
            
            await (Task) member.Invoke(this, new object[]{context.Caller, obj});
        }

        /// <inheritdoc/>
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            //TODO: For now just hard coded to Hash.Zero
            await smartContractMap.SetAsync(reg.ContractHash, reg.Serialize());
        }
        
        
        /// <inheritdoc/>
        public async Task DeploySmartContract(Hash account, SmartContractDeployment smartContractRegister)
        {
            var smartContractDeploymentMap =
                _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_INSTANCES);
            await smartContractDeploymentMap.SetAsync(smartContractRegister.ContractHash,
                smartContractRegister.Serialize());
        }

        public async Task<ISmartContract> GetSmartContractAsync(Hash hash)
        {
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            
            // get SmartContractRegistration
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            var reg = await smartContractMap.GetAsync(hash);
            
            // 
            var smartContractDeploymentMap =
                _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_INSTANCES);
            var deployment = await smartContractDeploymentMap.GetAsync(hash);

            if (reg == null || deployment == null)
                return null;
            
            var reg = SmartContractRegistration.Parser.ParseFrom(obj);

            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            var smartContract = await runner.RunAsync(reg);
            _smartContracts[hash] = smartContract;

            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;
        }

        public Hash GetHash()
        {
            return Hash.Zero;
        }

        /// <summary>
        /// deploy a contract account
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="smartContractRegistration"></param>
        /// <returns></returns>
        public Task<IAccount> DeployAccount(Hash caller, SmartContractRegistration smartContractRegistration)
        {
            // create new account for the contract
            var calllerContext =
                _accountContextService.GetAccountDataContext(caller, _accountDataProvider.Context.ChainId);
            throw new NotImplementedException();
            //var hash = new Hash(calllerContext.CalculateHashWith(smartContractRegistration.Bytes));
            //_accountContextService.GetAccountDataContext(hash, _accountDataProvider.Context.ChainId);
            //return Task.FromResult((IAccount) new Account(hash));
        }
    }
}