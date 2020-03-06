using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract;
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
        private readonly ISmartContractCodeHashProvider _smartContractCodeHashProvider;
        private readonly ISmartContractRegistrationCacheProvider _smartContractRegistrationCacheProvider;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        private readonly ISmartContractHeightInfoProvider _smartContractHeightInfoProvider; 
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        //TODO: there are too many injections here.
        public SmartContractExecutiveService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService, 
            ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider,
             ISmartContractExecutiveProvider smartContractExecutiveProvider, 
            ISmartContractHeightInfoProvider smartContractHeightInfoProvider, 
            ISmartContractCodeHashProvider smartContractCodeHashProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _smartContractRegistrationCacheProvider = smartContractRegistrationCacheProvider;
             _smartContractExecutiveProvider = smartContractExecutiveProvider;
             _smartContractHeightInfoProvider = smartContractHeightInfoProvider;
             _smartContractCodeHashProvider = smartContractCodeHashProvider;

             Logger = new NullLogger<SmartContractExecutiveService>();
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

            if (!pool.TryTake(out var executive) || _smartContractHeightInfoProvider.TryGetValue(address, out _))
            {
                var smartContractRegistration = await GetSmartContractRegistrationAsync(chainContext, address);
                if(executive == null || smartContractRegistration.CodeHash != executive.ContractHash)
                    executive = await GetExecutiveAsync(smartContractRegistration);
            }

            return executive;
        }

        public virtual async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_smartContractExecutiveProvider.TryGetValue(address, out var pool))
            {
                if (_smartContractRegistrationCacheProvider.TryGetValue(address, out var reg))
                {
                    if (reg.CodeHash == executive.ContractHash)
                    {
                        executive.LastUsedTime = TimestampHelper.GetUtcNow();
                        pool.Add(executive);
                        return;
                    }
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
        
        public void AddContractInfo(Address address, long blockHeight)
        {
            if (blockHeight <= Constants.GenesisBlockHeight) return;
            _smartContractRegistrationCacheProvider.TryRemove(address, out _);
            //TODO:if system crashed here, will it recovery?
            _smartContractExecutiveProvider.TryRemove(address, out _);
            //TODO:if system crashed here, will it recovery?
            if (!_smartContractHeightInfoProvider.TryGetValue(address, out var height) || blockHeight > height)
                _smartContractHeightInfoProvider.Set(address, blockHeight);
        }

        public void ClearContractInfo(long height)
        {
            var removeKeys = new List<Address>();
            foreach (var contractInfo in _smartContractHeightInfoProvider.GetContractInfos())
            {
                if (contractInfo.Value <= height) removeKeys.Add(contractInfo.Key);
            }

            foreach (var key in removeKeys)
            {
                _smartContractHeightInfoProvider.TryRemove(key, out _);
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
            if (_smartContractRegistrationCacheProvider.TryGetValue(address, out var smartContractRegistration))
            {
                if (!_smartContractHeightInfoProvider.TryGetValue(address, out var blockHeight)) return smartContractRegistration;
                
                //if contract has smartContractRegistration cache and update height. we need to get code hash in block
                //executed cache to check whether it is equal to the one in cache.
                var codeHash = await _smartContractCodeHashProvider.GetSmartContractCodeHashAsync(chainContext, address);
                if (smartContractRegistration.CodeHash != codeHash || blockHeight == chainContext.BlockHeight + 1)
                {
                    //registration is null or registration's code hash isn't equal to cache's code hash
                    //or current height is equal to update height.maybe the cache is wrong. we need to get
                    // smartContractRegistration from db to check whether cache's code hash is right.
                    var registrationInDb = await GetSmartContractRegistrationWithoutCacheAsync(chainContext, address);
                    if (smartContractRegistration.CodeHash == registrationInDb.CodeHash) return registrationInDb;
                    _smartContractRegistrationCacheProvider.Set(address, registrationInDb);
                    _smartContractExecutiveProvider.TryRemove(address, out _);
                    return registrationInDb;
                }

                return smartContractRegistration;
            }

            smartContractRegistration = await GetSmartContractRegistrationWithoutCacheAsync(chainContext, address);

            return smartContractRegistration;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationWithoutCacheAsync(
            IChainContext chainContext, Address address)
        {
            SmartContractRegistration smartContractRegistration;

            if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
            {
                smartContractRegistration = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                if (chainContext.BlockHeight > Constants.GenesisBlockHeight)
                {
                    //if Height > GenesisBlockHeight, maybe there is a new zero contract,
                    //the current smartContractRegistration is from code,
                    //not from zero contract, so we need to load new zero contract from the old smartContractRegistration,
                    //and replace it
                    var executiveZero = await GetExecutiveAsync(smartContractRegistration);
                    smartContractRegistration =
                        await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, address);
                }
            }
            else
            {
                smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(chainContext, address);
            }

            _smartContractRegistrationCacheProvider.TryAdd(address, smartContractRegistration);
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

            throw new SmartContractFindRegistrationException(
                $"failed to find registration from zero contract {txCtxt.Trace.Error}");
        }

    }
}