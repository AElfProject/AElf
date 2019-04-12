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
    /// <summary>
    /// a smart contract executive, don't use it out of AElf.Kernel.SmartContract
    /// </summary>
    public interface ISmartContractExecutiveService
    {
        Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address);

        Task PutExecutiveAsync(Address address, IExecutive executive);
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
                executive = await GetExecutiveAsync(address, reg);

                if (chainContext.BlockHeight > KernelConstants.GenesisBlockHeight && //already register zero to zero
                    address == _defaultContractZeroCodeProvider.ContractZeroAddress &&
                    !_addressSmartContractRegistrationMappingCache.ContainsKey(address)
                )
                {
                    //executive's registration is from code, not from contract
                    reg = await GetSmartContractRegistrationFromZeroAsync(executive, chainContext, address);
                    _addressSmartContractRegistrationMappingCache.TryAdd(address, reg);
                    executive = await GetExecutiveAsync(address, reg);
                }
            }

            return executive;
        }

        private async Task<IExecutive> GetExecutiveAsync(Address address, SmartContractRegistration reg)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

            // run smartcontract executive info and return executive
            var executive = await runner.RunAsync(reg);
            //executive.ContractHash = reg.CodeHash;
            //executive.ContractAddress = address;
            var context =
                _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(context);
            return executive;
        }


        public async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            GetPool(address).Add(executive);
            await Task.CompletedTask;
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
                _addressSmartContractRegistrationMappingCache.TryAdd(address, smartContractRegistration);
            }

            return smartContractRegistration;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address)
        {
            IExecutive executiveZero = null;
            try
            {
                executiveZero =
                    await GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
                return await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, address);
            }
            finally
            {
                if (executiveZero != null)
                {
                    await PutExecutiveAsync(_defaultContractZeroCodeProvider.ContractZeroAddress, executiveZero);
                }
            }
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IExecutive executiveZero, IChainContext chainContext, Address address)
        {
            var transaction = new Transaction()
            {
                From = Address.Zero,
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "GetSmartContractRegistrationByAddress",
                Params = address.ToByteString()
            };

            var trace = new TransactionTrace
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
                StateCache = chainContext.StateCache
            };

            await executiveZero.ApplyAsync(txCtxt);
            var returnBytes = txCtxt.Trace?.ReturnValue;
            if (returnBytes != null && returnBytes != ByteString.Empty)
            {
                return SmartContractRegistration.Parser.ParseFrom(returnBytes);
            }

            throw new InvalidOperationException(
                $"failed to find registration from zero contract {txCtxt.Trace.StdErr}");
        }

        #endregion
    }
}