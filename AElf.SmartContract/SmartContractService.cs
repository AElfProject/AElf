using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.ABI.CSharp;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using Google.Protobuf;
using AElf.Kernel;
using AElf.Configuration;
using AElf.Types.CSharp;
using Type = System.Type;
using AElf.Common;
using AElf.Kernel.Storages;
using Akka.Util.Internal;

namespace AElf.SmartContract
{
    public class SmartContractService : ISmartContractService
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly ConcurrentDictionary<Address, ConcurrentBag<IExecutive>> _executivePools = new ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>();
        private readonly ConcurrentDictionary<Address, Hash> _contractHashs = new ConcurrentDictionary<Address, Hash>();
        private readonly IStateStore _stateStore;
        private readonly IFunctionMetadataService _functionMetadataService;

        public SmartContractService(ISmartContractManager smartContractManager, ISmartContractRunnerFactory smartContractRunnerFactory, IStateStore stateStore,
            IFunctionMetadataService functionMetadataService)
        {
            _smartContractManager = smartContractManager;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _stateStore = stateStore;
            _functionMetadataService = functionMetadataService;
        }

        private ConcurrentBag<IExecutive> GetPoolFor(Address account)
        {
            if (!_executivePools.TryGetValue(account, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[account] = pool;
            }

            return pool;
        }

        public void ClearPool(Address address)
        {
            if (_executivePools.ContainsKey(address))
            {
                _executivePools[address] = new ConcurrentBag<IExecutive>();
            }
        }

        private Hash GetContractVersion(Address address)
        {
            if (!_contractHashs.TryGetValue(address, out var hash))
            {
                hash = _smartContractManager.GetAsync(address).Result.ContractHash;
                _contractHashs.TryAdd(address, hash);
            }
            return hash;
        }

        public async Task<IExecutive> GetExecutiveAsync(Address contractAddress, Hash chainId)
        {
            var pool = GetPoolFor(contractAddress);
            if (pool.TryTake(out var executive))
                return executive;

            // get registration
            var reg = await _smartContractManager.GetAsync(contractAddress);

            // get runner
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);

            if (runner == null)
            {
                throw new NotSupportedException($"Runner for category {reg.Category} is not registered.");
            }

            // get account dataprovider
            var dataProvider = DataProvider.GetRootDataProvider(chainId, contractAddress);
            dataProvider.StateStore = _stateStore;
            // run smartcontract executive info and return executive

            executive = await runner.RunAsync(reg);
            executive.ContractHash = reg.ContractHash;
            executive.SetStateStore(_stateStore);
            
            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = chainId,
                ContractAddress = contractAddress,
                DataProvider = dataProvider,
                SmartContractService = this
            });

            return executive;
        }

        public async Task PutExecutiveAsync(Address account, IExecutive executive)
        {
            if (executive.ContractHash.Equals(GetContractVersion(account)))
            {
                executive.SetTransactionContext(new TransactionContext());
                executive.SetDataCache(new Dictionary<DataPath, StateCache>());
                GetPoolFor(account).Add(executive);
            }

            await Task.CompletedTask;
        }

        private Type GetContractType(SmartContractRegistration registration)
        {
            var runner = _smartContractRunnerFactory.GetRunner(registration.Category);
            if (runner == null)
            {
                throw new NotSupportedException($"Runner for category {registration.Category} is not registered.");
            }
            return runner.GetContractType(registration);
        }
        
        /// <inheritdoc/>
        public async Task DeployContractAsync(Hash chainId, Address contractAddress, SmartContractRegistration registration, bool isPrivileged)
        {
            // get runnner
            var runner = _smartContractRunnerFactory.GetRunner(registration.Category);
            runner.CodeCheck(registration.ContractBytes.ToByteArray(), isPrivileged);

            if (ParallelConfig.Instance.IsParallelEnable)
            {
                var contractType = runner.GetContractType(registration);
                var contractTemplate = runner.ExtractMetadata(contractType);
                await _functionMetadataService.DeployContract(chainId, contractAddress, contractTemplate);
            }
            
            await _smartContractManager.InsertAsync(contractAddress, registration);
            _contractHashs.AddOrSet(contractAddress, registration.ContractHash);
        }
        
        public async Task UpdateContractAsync(Hash chainId, Address contractAddress, SmartContractRegistration newRegistration, bool isPrivileged)
        {
            // get runnner
            var runner = _smartContractRunnerFactory.GetRunner(newRegistration.Category);
            runner.CodeCheck(newRegistration.ContractBytes.ToByteArray(), isPrivileged);

            if (ParallelConfig.Instance.IsParallelEnable)
            {
                var oldRegistration = await _smartContractManager.GetAsync(contractAddress);
                var oldContractType = runner.GetContractType(oldRegistration);
                var oldContractTemplate = runner.ExtractMetadata(oldContractType);
                
                var newContractType = runner.GetContractType(newRegistration);
                var newContractTemplate = runner.ExtractMetadata(newContractType);
                await _functionMetadataService.UpdateContract(chainId, contractAddress, newContractTemplate, oldContractTemplate);
            }
            await _smartContractManager.InsertAsync(contractAddress, newRegistration);
            
            _contractHashs.AddOrSet(contractAddress, newRegistration.ContractHash);
            ClearPool(contractAddress);
        }

        public async Task<IMessage> GetAbiAsync(Address account)
        {
            var reg = await _smartContractManager.GetAsync(account);
            return GetAbiAsync(reg);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetInvokingParams(Transaction transaction)
        {
            var reg = await _smartContractManager.GetAsync(transaction.To);
            var abi = (Module) GetAbiAsync(reg);
            
            // method info 
            var methodInfo = GetContractType(reg).GetMethod(transaction.MethodName);
            var parameters = ParamsPacker.Unpack(transaction.Params.ToByteArray(),
                methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
            // get method in abi
            var method = abi.Methods.First(m => m.Name.Equals(transaction.MethodName));
            
            // deserialize
            return method.DeserializeParams(parameters);
        }

        private IMessage GetAbiAsync(SmartContractRegistration reg)
        {
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            return runner.GetAbi(reg);
        }
    }
}
