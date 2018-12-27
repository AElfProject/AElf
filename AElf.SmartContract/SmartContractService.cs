using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AElf.ABI.CSharp;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using Google.Protobuf;
using AElf.Kernel;
using AElf.Configuration;
using AElf.Types.CSharp;
using Type = System.Type;
using AElf.Common;
using Akka.Util.Internal;

namespace AElf.SmartContract
{
    public class SmartContractService : ISmartContractService
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools = new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
        private readonly IStateManager _stateManager;
        private readonly IFunctionMetadataService _functionMetadataService;

        public SmartContractService(ISmartContractManager smartContractManager, ISmartContractRunnerContainer smartContractRunnerContainer, IStateManager stateManager,
            IFunctionMetadataService functionMetadataService)
        {
            _smartContractManager = smartContractManager;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _stateManager = stateManager;
            _functionMetadataService = functionMetadataService;
        }

        private async Task<ConcurrentBag<IExecutive>> GetPoolForAsync(Hash chainId, Address account)
        {
            var contractHash = await GetContractHashAsync(chainId, account);
            if (!_executivePools.TryGetValue(contractHash, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[contractHash] = pool;
            }

            return pool;
        }

        private async Task<Hash> GetContractHashAsync(Hash chainId, Address address)
        {
            Hash contractHash;
            var zeroContractAdress = ContractHelpers.GetGenesisBasicContractAddress(chainId);

            if (address == zeroContractAdress)
            {
                contractHash = Hash.FromMessage(zeroContractAdress);
            }
            else
            {
                var result = CallContract(chainId, ContractHelpers.GetGenesisBasicContractAddress(chainId),
                    "GetContractHash", address);

                contractHash = result.DeserializeToPbMessage<Hash>();
            }

            return contractHash;
        }

        public async Task<SmartContractRegistration> GetContractByAddressAsync(Hash chainId, Address address)
        {
            var contractHash = await GetContractHashAsync(chainId, address);
            return await _smartContractManager.GetAsync(contractHash);
        }

        public async Task<IExecutive> GetExecutiveAsync(Address contractAddress, Hash chainId)
        {
            var pool = await GetPoolForAsync(chainId, contractAddress);
            if (pool.TryTake(out var executive))
                return executive;

            // get registration
            var reg = await GetContractByAddressAsync(chainId, contractAddress);

            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

            if (runner == null)
            {
                throw new NotSupportedException($"Runner for category {reg.Category} is not registered.");
            }

            // get account dataprovider
            var dataProvider = DataProvider.GetRootDataProvider(chainId, contractAddress);
            dataProvider.StateManager = _stateManager;
            // run smartcontract executive info and return executive

            executive = await runner.RunAsync(reg);
            executive.ContractHash = reg.ContractHash;
            executive.SetStateManager(_stateManager);
            
            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = chainId,
                ContractAddress = contractAddress,
                DataProvider = dataProvider,
                SmartContractService = this
            });

            return executive;
        }

        public async Task PutExecutiveAsync(Hash chainId, Address account, IExecutive executive)
        {
            executive.SetTransactionContext(new TransactionContext());
            executive.SetDataCache(new Dictionary<StatePath, StateCache>());
            (await GetPoolForAsync(chainId, account)).Add(executive);

            await Task.CompletedTask;
        }

        private Type GetContractType(SmartContractRegistration registration)
        {
            var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
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
            var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
            runner.CodeCheck(registration.ContractBytes.ToByteArray(), isPrivileged);

            //Todo handle metadata
            if (ParallelConfig.Instance.IsParallelEnable)
            {
                var contractType = runner.GetContractType(registration);
                var contractTemplate = runner.ExtractMetadata(contractType);
                await _functionMetadataService.DeployContract(chainId, contractAddress, contractTemplate);
            }
            
            await _smartContractManager.InsertAsync(registration);
        }
        
        public async Task UpdateContractAsync(Hash chainId, Address contractAddress, SmartContractRegistration newRegistration, bool isPrivileged)
        {
            // get runnner
            var runner = _smartContractRunnerContainer.GetRunner(newRegistration.Category);
            runner.CodeCheck(newRegistration.ContractBytes.ToByteArray(), isPrivileged);

            //Todo handle metadata
            if (ParallelConfig.Instance.IsParallelEnable)
            {
                var oldRegistration = await GetContractByAddressAsync(chainId, contractAddress);
                var oldContractType = runner.GetContractType(oldRegistration);
                var oldContractTemplate = runner.ExtractMetadata(oldContractType);
                
                var newContractType = runner.GetContractType(newRegistration);
                var newContractTemplate = runner.ExtractMetadata(newContractType);
                await _functionMetadataService.UpdateContract(chainId, contractAddress, newContractTemplate, oldContractTemplate);
            }
            await _smartContractManager.InsertAsync(newRegistration);
        }

        public async Task<IMessage> GetAbiAsync(Hash chainId, Address account)
        {
            var reg = await GetContractByAddressAsync(chainId, account);
            return GetAbiAsync(reg);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetInvokingParams(Hash chainId, Transaction transaction)
        {
            var reg = await GetContractByAddressAsync(chainId, transaction.To);
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
            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);
            return runner.GetAbi(reg);
        }
        
        public async Task DeployZeroContractAsync(Hash chainId, SmartContractRegistration registration)
        {
            registration.ContractHash = Hash.FromMessage(ContractHelpers.GetGenesisBasicContractAddress(chainId));

            await _smartContractManager.InsertAsync(registration);
        }

        public async Task<Address> DeploySystemContractAsync(Hash chainId, ulong serialNumber, int category, byte[] code)
        {
            var result = CallContract(chainId, ContractHelpers.GetGenesisBasicContractAddress(chainId),
                "InitSmartContract", serialNumber, category, code);

            return result.DeserializeToPbMessage<Address>();
        }
        
        private byte[] CallContract(Hash chainId, Address contractAddress, string methodName, params object[] args)
        {
            var smartContractContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = contractAddress,
                    To = contractAddress,
                    MethodName = methodName,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
                }
            };

            Task.Factory.StartNew(async () =>
            {
                var executive = await GetExecutiveAsync(contractAddress, chainId);
                var dataProvider = DataProvider.GetRootDataProvider(chainId, contractAddress);
                dataProvider.StateManager = _stateManager;
                executive.SetDataCache(dataProvider.StateCache);
                try
                {
                    await executive.SetTransactionContext(smartContractContext).Apply();
                }
                finally
                {
                    await PutExecutiveAsync(chainId, contractAddress, executive);
                }
            }).Unwrap().Wait();
            
            if (smartContractContext.Trace.IsSuccessful())
            {
                if (smartContractContext.Trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
                {
                    smartContractContext.Trace.CommitChangesAsync(_stateManager);
                }
            }

            return smartContractContext.Trace.RetVal.Data.ToByteArray();
        }
    }
}
