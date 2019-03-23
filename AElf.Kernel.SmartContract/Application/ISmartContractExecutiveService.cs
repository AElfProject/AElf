using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveService
    {
        Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address);

        Task PutExecutiveAsync(Address address, IExecutive executive);

        Task<IMessage> GetAbiAsync(IChainContext chainContext, Address address);
    }

    public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IStateProviderFactory _stateProviderFactory;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

        private readonly ConcurrentDictionary<Address, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>();

        private readonly ConcurrentDictionary<Address, SmartContractRegistration>
            _addressSmartContractRegistrationMappingCache =
                new ConcurrentDictionary<Address, SmartContractRegistration>();
#if DEBUG
        public ILogger<ISmartContractContext> SmartContractContextLogger { get; set; }
#endif

        public SmartContractExecutiveService(
            ISmartContractRunnerContainer smartContractRunnerContainer, IStateProviderFactory stateProviderFactory,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _stateProviderFactory = stateProviderFactory;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
        }

        private ConcurrentBag<IExecutive> GetPool(Address address)
        {
            if (!_executivePools.TryGetValue(address, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[address] = pool;
            }

            return pool;
        }

        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var pool = GetPool(address);

            if (!pool.TryTake(out var executive))
            {
                var reg = await GetSmartContractRegistrationAsync(chainContext, address);
                // get runner
                var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

                // run smartcontract executive info and return executive
                executive = await runner.RunAsync(reg);
                executive.ContractHash = reg.CodeHash;
                executive.ContractAddress = address;
                executive.SetHostSmartContractBridgeContext(
                    _hostSmartContractBridgeContextService.Create(
                        new SmartContractContext() {ContractAddress = address}));
            }

            executive.SetStateProviderFactory(_stateProviderFactory);
            return executive;
        }

        public async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            executive.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    To = address // This is to ensure that the contract has same address
                }
            });
            executive.SetDataCache(new NullStateCache());
            GetPool(address).Add(executive);

            await Task.CompletedTask;
        }

        public async Task<IMessage> GetAbiAsync(IChainContext chainContext, Address address)
        {
            var smartContractRegistration = await GetSmartContractRegistrationAsync(chainContext, address);
            var runner = _smartContractRunnerContainer.GetRunner(smartContractRegistration.Category);
            return runner.GetAbi(smartContractRegistration);
        }

        #region private methods

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
            IChainContext chainContext, Address address)
        {
            if (_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var smartContractRegistration))
                return smartContractRegistration;

            if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
            {
                smartContractRegistration = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
            }
            else
            {
                smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(chainContext, address);
            }

            _addressSmartContractRegistrationMappingCache.TryAdd(address, smartContractRegistration);
            return smartContractRegistration;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address)
        {
            var transaction = new Transaction()
            {
                From = Address.Zero,
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "GetSmartContractRegistrationByAddress",
                Params = address.ToByteString()
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

            var registration = _defaultContractZeroCodeProvider
                .DefaultContractZeroRegistration;

            IExecutive executiveZero = null;
            SmartContractRegistration result = null;
            try
            {
                executiveZero =
                    await GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
                executiveZero.SetDataCache(chainContext.StateCache);
                await executiveZero.SetTransactionContext(txCtxt).Apply();
                var returnBytes = txCtxt.Trace?.ReturnValue;
                if (returnBytes != null && returnBytes != ByteString.Empty)
                {
                    result = SmartContractRegistration.Parser.ParseFrom(returnBytes);
                }
            }
            finally
            {
                if (executiveZero != null)
                {
                    await PutExecutiveAsync(_defaultContractZeroCodeProvider.ContractZeroAddress, executiveZero);
                }
            }

            return result;
        }

        #endregion
    }
}