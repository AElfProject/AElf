using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
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
        private Hash _initLibBlockHash = Hash.Empty;
        private long _initLibBlockHeight;

        private const int ExecutiveExpirationTime = 3600; // 1 Hour
        private const int ExecutiveClearLimit = 10;

        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractRegistrationCacheProvider _smartContractRegistrationCacheProvider;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        public SmartContractExecutiveService(IDeployedContractAddressProvider deployedContractAddressProvider,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
            IChainBlockLinkService chainBlockLinkService, IBlockchainService blockchainService,
            ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider,
            ISmartContractExecutiveProvider smartContractExecutiveProvider)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _chainBlockLinkService = chainBlockLinkService;
            _blockchainService = blockchainService;
            _smartContractRegistrationCacheProvider = smartContractRegistrationCacheProvider;
            _smartContractExecutiveProvider = smartContractExecutiveProvider;

            Logger = NullLogger<SmartContractExecutiveService>.Instance;
        }

        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var reg = await GetSmartContractRegistrationAsync(chainContext, address);
            var pool = _smartContractExecutiveProvider.GetPool(address, reg.CodeHash);

            if (!pool.TryTake(out var executive))
            {
                executive = await GetExecutiveAsync(reg);
            }

            return executive;
        }

        public async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_smartContractExecutiveProvider.TryGetExecutiveDictionary(address, out var dictionary))
            {
                if (dictionary.TryGetValue(executive.ContractHash, out var pool))
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
            var executivePools = _smartContractExecutiveProvider.GetExecutivePools();
            foreach (var executivePool in executivePools)
            {
                foreach (var executiveBag in executivePool.Value.Values)
                {
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

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,
            Address address)
        {
            var registrationCache = GetSmartContractRegistrationCacheFromForkCache(chainContext, address);

            if (registrationCache != null)
            {
                return await GetSmartContractRegistrationAsync(registrationCache, address);
            }

            registrationCache = await GetSmartContractRegistrationCacheFromLibCache(chainContext, address);
            return await GetSmartContractRegistrationAsync(registrationCache, address, chainContext.StateCache);
        }

        private SmartContractRegistrationCache GetSmartContractRegistrationCacheFromForkCache(
            IChainContext chainContext, Address address)
        {
            if (!_smartContractRegistrationCacheProvider.TryGetForkCache(address, out var caches)) return null;
            var cacheList = caches.ToList();
            if (cacheList.Count == 0) return null;
            var minHeight = cacheList.Min(s => s.BlockHeight);
            var blockHashes = cacheList.Select(s => s.BlockHash).ToList();
            var blockHash = chainContext.BlockHash;
            var blockHeight = chainContext.BlockHeight;
            do
            {
                if (blockHashes.Contains(blockHash)) return cacheList.Last(s => s.BlockHash == blockHash);

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockHash);
                blockHash = link?.PreviousBlockHash;
                blockHeight--;
            } while (blockHash != null && blockHeight >= minHeight);

            return null;
        }

        private async Task<SmartContractRegistrationCache> GetSmartContractRegistrationCacheFromLibCache(
            IChainContext chainContext, Address address)
        {
            if (_smartContractRegistrationCacheProvider.TryGetLibCache(address,
                out var smartContractRegistrationCache)) return smartContractRegistrationCache;

            if (chainContext.BlockHeight > 0 && _initLibBlockHeight == 0)
            {
                var chain = await _blockchainService.GetChainAsync();
                _initLibBlockHash = chain.LastIrreversibleBlockHash;
                _initLibBlockHeight = chain.LastIrreversibleBlockHeight;
            }

            //Use lib chain context to set lib cache. Genesis block need to execute with state cache
            var context = new ChainContext
            {
                BlockHash = _initLibBlockHash,
                BlockHeight = _initLibBlockHeight,
                StateCache = chainContext.BlockHeight == 0 ? chainContext.StateCache : null
            };
            if (!_deployedContractAddressProvider.CheckContractAddress(context, address))
                return null;
            SmartContractRegistration smartContractRegistration;
            if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
            {
                smartContractRegistration = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                if (context.BlockHeight > Constants.GenesisBlockHeight)
                {
                    var executive = await GetExecutiveAsync(smartContractRegistration);
                    smartContractRegistration =
                        await GetSmartContractRegistrationFromZeroAsync(executive, context, address);
                }
            }
            else
            {
                smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(context, address);
            }

            smartContractRegistrationCache = new SmartContractRegistrationCache
            {
                SmartContractRegistration = smartContractRegistration,
                BlockHash = context.BlockHash,
                BlockHeight = context.BlockHeight,
                Address = address
            };
            _smartContractRegistrationCacheProvider.SetLibCache(address, smartContractRegistrationCache);
            return smartContractRegistrationCache;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
            SmartContractRegistrationCache smartContractRegistrationCache, Address address,
            IStateCache stateCache = null)
        {
            //Cannot find registration in fork cache and lib cache
            if (smartContractRegistrationCache == null)
            {
                //Check whether stateCache has smartContract registration
                var smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(new ChainContext
                {
                    BlockHash = _initLibBlockHash,
                    BlockHeight = _initLibBlockHeight,
                    StateCache = stateCache
                }, address);
                if (smartContractRegistration == null)
                    throw new SmartContractFindRegistrationException("failed to find registration from zero contract");
                return smartContractRegistration;
            }

            if (smartContractRegistrationCache.SmartContractRegistration.Code.IsEmpty)
            {
                smartContractRegistrationCache.SmartContractRegistration =
                    await GetSmartContractRegistrationFromZeroAsync(new ChainContext
                        {
                            BlockHash = smartContractRegistrationCache.BlockHash,
                            BlockHeight = smartContractRegistrationCache.BlockHeight
                        },
                        smartContractRegistrationCache.Address);
            }

            return smartContractRegistrationCache.SmartContractRegistration;
        }
        
        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address)
        {
            IExecutive executiveZero = null;
            try
            {
                if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
                {
                    executiveZero =
                        await GetExecutiveAsync(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration);
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
                    await PutExecutiveAsync(_defaultContractZeroCodeProvider.ContractZeroAddress, executiveZero);
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

            if (!txCtxt.Trace.IsSuccessful())
                throw new SmartContractFindRegistrationException(
                    $"failed to find registration from zero contract {txCtxt.Trace.Error}");
            return null;
        }
    }
}