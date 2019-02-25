using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveService
    {
        Task<IExecutive> GetExecutiveAsync(int chainId, IChainContext chainContext, Address address);
        Task<IExecutive> GetExecutiveAsync(SmartContractRegistration reg);

        Task PutExecutiveAsync(int chainId, Address address, IExecutive executive);

        Task<IMessage> GetAbiAsync(int chainId, IChainContext chainContext, Address address);
//
//        Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address);
    }

    public class SmartContractExecutiveService : ISmartContractExecutiveService, ITransientDependency
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IStateProviderFactory _stateProviderFactory;
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();

        public SmartContractExecutiveService(IServiceProvider serviceProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer, IStateProviderFactory stateProviderFactory,
            ISmartContractManager smartContractManager,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _serviceProvider = serviceProvider;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _stateProviderFactory = stateProviderFactory;
            _smartContractManager = smartContractManager;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }

//        private async Task<Hash> GetContractHashAsync(int chainId, Address address)
//        {
//            Hash contractHash;
//            var zeroContractAddress = ContractHelpers.GetGenesisBasicContractAddress(chainId);
//
//            if (address == zeroContractAddress)
//            {
//                contractHash = Hash.FromMessage(zeroContractAddress);
//            }
//            else
//            {
//                var result = await CallContractAsync(true, chainId, zeroContractAddress, "GetContractHash", address);
//
//                contractHash = result.DeserializeToPbMessage<Hash>();
//            }
//
//            return contractHash;
//        }

//        public async Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address)
//        {
//            var contractHash = await GetContractHashAsync(chainId, address);
//            return await _smartContractManager.GetAsync(contractHash);
//        }
        private async Task<ConcurrentBag<IExecutive>> GetPoolForAsync(Hash contractHash)
        {
            if (!_executivePools.TryGetValue(contractHash, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[contractHash] = pool;
            }

            return pool;
        }

        public async Task<IExecutive> GetExecutiveAsync(int chainId, IChainContext chainContext, Address address)
        {
            var reg = await GetSmartContractRegistrationAsync(chainId, chainContext, address);
            var executive = await GetExecutiveAsync(reg);

            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = chainId,
                ContractAddress = address,
                ChainService = _serviceProvider.GetService<IBlockchainService>(),
                SmartContractService = _serviceProvider.GetService<ISmartContractService>(),
                SmartContractExecutiveService = this
            });

            return executive;
        }

        public async Task PutExecutiveAsync(int chainId, Address address, IExecutive executive)
        {
            executive.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    To = address // This is to ensure that the contract has same address
                }
            });
            executive.SetDataCache(new Dictionary<StatePath, StateCache>());
            (await GetPoolForAsync(executive.ContractHash)).Add(executive);

            await Task.CompletedTask;
        }

        public async Task<IMessage> GetAbiAsync(int chainId, IChainContext chainContext, Address address)
        {
            var smartContractRegistration = await GetSmartContractRegistrationAsync(chainId, chainContext, address);
            var runner = _smartContractRunnerContainer.GetRunner(smartContractRegistration.Category);
            return runner.GetAbi(smartContractRegistration);
        }

        public async Task<IExecutive> GetExecutiveAsync(SmartContractRegistration reg)
        {
            var pool = await GetPoolForAsync(reg.CodeHash);

            if (!pool.TryTake(out var executive))
            {
                // get runner
                var runner = _smartContractRunnerContainer.GetRunner(reg.Category);
                if (runner == null)
                {
                    throw new NotSupportedException($"Runner for category {reg.Category} is not registered.");
                }

                // run smartcontract executive info and return executive
                executive = await runner.RunAsync(reg);
                executive.ContractHash = reg.CodeHash;
            }

            executive.SetStateProviderFactory(_stateProviderFactory);
            return executive;
        }

        #region private methods

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(int chainId,
            IChainContext chainContext, Address address)
        {
            if (address == Address.BuildContractAddress(chainId, 0))
            {
                return _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
            }
            var hash = await GetContractHashFromZeroAsync(chainId, chainContext, address);    

            return await _smartContractManager.GetAsync(hash);
        }

        private async Task<Hash> GetContractHashFromZeroAsync(int chainId, IChainContext chainContext, Address address)
        {
            var transaction = new Transaction()
            {
                From = Address.Zero,
                To = Address.BuildContractAddress(chainId, 0),
                MethodName = "GetContractInfo",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(address))
            };
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = DateTime.UtcNow,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
            };
            var registration = await _smartContractManager.GetAsync(_defaultContractZeroCodeProvider
                .DefaultContractZeroRegistration.CodeHash);
            var executiveZero = await GetExecutiveAsync(registration);
            await executiveZero.SetTransactionContext(txCtxt).Apply();
            return Hash.LoadHex(
                ((JObject) JsonConvert.DeserializeObject(trace.RetVal.Data.DeserializeToString()))["CodeHash"]
                .ToString());
        }

        #endregion
    }
}