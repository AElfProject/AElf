using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
    {
        private const int ExecutiveExpirationTime = 3600; // 1 Hour
        private const int ExecutiveClearLimit = 10;

        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        //TODO: there are too many injections here.
        public SmartContractExecutiveService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService, 
            ISmartContractRegistrationProvider smartContractRegistrationProvider,
            ISmartContractExecutiveProvider smartContractExecutiveProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _smartContractRegistrationProvider = smartContractRegistrationProvider;
             _smartContractExecutiveProvider = smartContractExecutiveProvider;
             
            Logger = NullLogger<SmartContractExecutiveService>.Instance;
        }

        //TODO: 1. check in the pool.
        //2.A if not in the pool, get from SmartContractRegistrationCacheProvider.Get(chainContext,address)
        //    SmartContractRegistrationCacheProvider.Get(chainContext,address) => BlockchainStateService.GetExecutedData(chainContext,key)
        //    And in my view, you can also implement a general MemoryCacheProvider for BlockchainStateService.GetExecutedData
        //2.B if in the pool, compare executive.Hash with SmartContractRegistrationCacheProvider.Get(chainContext,address).CodeHash.
        //    if not the same, clean the pool, and try 2.A
        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var pool = _smartContractExecutiveProvider.GetPool(address);
            var smartContractRegistration = await GetSmartContractRegistrationAsync(chainContext, address);

            if (!pool.TryTake(out var executive) )
            {
                executive = await GetExecutiveAsync(smartContractRegistration);
            }
            else if(smartContractRegistration.CodeHash != executive.ContractHash)
            {
                _smartContractExecutiveProvider.TryRemove(address, out _);
                executive = await GetExecutiveAsync(smartContractRegistration);
            }

            return executive;
        }

        public virtual async Task PutExecutiveAsync(IChainContext chainContext, Address address, IExecutive executive)
        {
            if (_smartContractExecutiveProvider.TryGetValue(address, out var pool))
            {
                var smartContractRegistration =
                    await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext, address);
                if (smartContractRegistration != null && smartContractRegistration.CodeHash == executive.ContractHash ||
                    chainContext.BlockHeight <= Constants.GenesisBlockHeight)
                {
                    executive.LastUsedTime = TimestampHelper.GetUtcNow();
                    pool.Add(executive);
                    return;
                }

                Logger.LogDebug($"Lost an executive (no registration {address})");
            }
            else
            {
                Logger.LogDebug($"Lost an executive (no pool {address})");
            }

            await Task.CompletedTask;
        }

        public void CleanIdleExecutive()
        {
            foreach (var executivePool in _smartContractExecutiveProvider.GetExecutivePools())
            {
                var executiveBag = executivePool.Value;
                if (executiveBag.Count > ExecutiveClearLimit && executiveBag.Min(o => o.LastUsedTime) <
                    TimestampHelper.GetUtcNow() - TimestampHelper.DurationFromSeconds(ExecutiveExpirationTime))
                {
                    if (executiveBag.TryTake(out _))
                    {
                        Logger.LogDebug($"Cleaned an idle executive for address {executivePool.Key}.");
                    }
                }
            }
        }

        private async Task<IExecutive> GetExecutiveAsync(SmartContractRegistration reg)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

            // run smartContract executive info and return executive
            var executive = await runner.RunAsync(reg);

            var context =
                _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(context);
            return executive;
        }
        
        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
            IChainContext chainContext, Address address)
        {
            var smartContractRegistration =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext, address);
            if (smartContractRegistration != null) return smartContractRegistration;
            
            smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(chainContext, address);

            return smartContractRegistration;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address)
        {
            IExecutive executiveZero = null;
            try
            {
                if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
                {
                    var smartContractRegistration = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                    if (chainContext.BlockHeight <= Constants.GenesisBlockHeight) return smartContractRegistration;
                    //if Height > GenesisBlockHeight, maybe there is a new zero contract,
                    //the current smartContractRegistration is from code,
                    //not from zero contract, so we need to load new zero contract from the old smartContractRegistration,
                    //and replace it
                    executiveZero = await GetExecutiveAsync(smartContractRegistration);
                }
                else
                {
                    executiveZero =
                        await GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
                }
                
                return await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, address);
            }
            finally
            {
                if (executiveZero != null)
                {
                    await PutExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress,
                        executiveZero);
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

            throw new SmartContractFindRegistrationException(
                $"failed to find registration from zero contract {txCtxt.Trace.Error}");
        }

    }
}