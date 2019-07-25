using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
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

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

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


                if (address == _defaultContractZeroCodeProvider.ContractZeroAddress && 
                    !_addressSmartContractRegistrationMappingCache.ContainsKey(address))
                {
                    if (chainContext.BlockHeight > Constants.GenesisBlockHeight)
                    {
                        
                        //if Height > GenesisBlockHeight, maybe there is a new zero contract, the current executive is from code,
                        //not from zero contract, so we need to load new zero contract from the old executive,
                        //and replace it
                        reg = await GetSmartContractRegistrationFromZeroAsync(executive, chainContext, address);
                        executive = await GetExecutiveAsync(address, reg);
                    }
                    
                    //add cache for zero, because GetSmartContractRegistrationAsync method do not add zero cache
                    _addressSmartContractRegistrationMappingCache.TryAdd(address, reg);

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

            var context =
                _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(context);
            return executive;
        }


        public virtual async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var pool))
            {
                if (_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var reg))
                {
                    if (reg.CodeHash == executive.ContractHash)
                    {
                        pool.Add(executive);
                    }
                }
            }

            await Task.CompletedTask;
        }

        public void ClearExecutivePool(Address address)
        {
            _addressSmartContractRegistrationMappingCache.TryRemove(address, out _);
            _executivePools.TryRemove(address, out _);
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
                From = FromAddress,
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
                CurrentBlockTime = TimestampHelper.GetUtcNow(),
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
                $"failed to find registration from zero contract {txCtxt.Trace.Error}");
        }

        #endregion
    }
}