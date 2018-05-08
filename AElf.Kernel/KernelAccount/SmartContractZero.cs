using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;

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
        
        private readonly ISerializer<SmartContractRegistration> _serializer;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory,
            IWorldStateManager worldStateManager, ISerializer<SmartContractRegistration> serializer, 
            IAccountContextService accountContextService)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
            _serializer = serializer;
            _accountContextService = accountContextService;
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash caller, string methodname, ByteString bytes)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);
            var p = member.GetParameters()[0]; //first parameters
            
            ProtobufSerializer serializer=new ProtobufSerializer();
            var obj = serializer.Deserialize(bytes.ToByteArray(), p.ParameterType.DeclaringType);
            
            await (Task) member.Invoke(this, new object[]{caller, obj});
        }

        // Hard coded method in the kernel
        public async Task RegisterSmartContract(Hash caller, SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            //TODO: For now just hard coded to Hash.Zero
            await smartContractMap.SetAsync(reg.ContractHash, _serializer.Serialize(reg));
        }

        public async Task DeploySmartContract(Hash caller, SmartContractDeployment smartContractRegister)
        {
            var addresses = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_INSTANCES);
            
        }


        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            throw new NotImplementedException();
        }

        public async Task DeploySmartContract(SmartContractDeployment smartContractRegister)
        {
            throw new NotImplementedException();
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
             
            var acc = new Account(reg.ContractHash);
            var dp = _worldStateManager.GetAccountDataProvider(_accountDataProvider.Context.ChainId, acc.GetAddress());

            await smartContract.InitializeAsync(dp);

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