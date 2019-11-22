using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address);

        Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address);

        Task PutExecutiveAsync(IExecutive executive);

        void Init(Hash blockHash, long blockHeight);

        void AddSmartContractRegistration(Address address, Hash codeHash, BlockIndex blockIndex);

        void RemoveForkCache(List<BlockIndex> blockIndexes);

        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public class SmartContractExecutiveProvider : ISmartContractExecutiveProvider,ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, SmartContractRegistrationCache>
            _addressSmartContractRegistrationMappingCache =
                new ConcurrentDictionary<Address, SmartContractRegistrationCache>();

        private readonly ConcurrentDictionary<Address, List<SmartContractRegistrationCache>> _forkCache =
            new ConcurrentDictionary<Address, List<SmartContractRegistrationCache>>();

        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
        
        private Hash _initLibBlockHash = Hash.Empty;
        private long _initLibBlockHeight;

        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());


        public SmartContractExecutiveProvider(IDeployedContractAddressProvider deployedContractAddressProvider,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService, 
            IChainBlockLinkService chainBlockLinkService)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _chainBlockLinkService = chainBlockLinkService;
        }
        
        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address];
                var codeHashes = caches.Where(c => blockHashes.Contains(c.BlockHash))
                    .Select(c => c.SmartContractRegistration.CodeHash).ToList();
                caches.RemoveAll(cache => blockHashes.Contains(cache.BlockHash));
                ClearExecutives(codeHashes);
                if (caches.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address];
                foreach (var cache in caches)
                {
                    if (!blockHashes.Contains(cache.BlockHash) ||
                        _addressSmartContractRegistrationMappingCache.TryGetValue(cache.Address,
                            out var registrationCache) &&
                        registrationCache.SmartContractRegistration.CodeHash ==
                        cache.SmartContractRegistration.CodeHash)
                        continue;
                    _addressSmartContractRegistrationMappingCache[cache.Address] = cache;
                }
                var codeHashes = caches.Where(c => blockHashes.Contains(c.BlockHash))
                    .Select(c => c.SmartContractRegistration.CodeHash).ToList();
                caches.RemoveAll(cache => blockHashes.Contains(cache.BlockHash));
                ClearExecutives(codeHashes);
                if (caches.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }
        }

        private ConcurrentBag<IExecutive> GetPool(Hash codeHash)
        {
            if (!_executivePools.TryGetValue(codeHash, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[codeHash] = pool;
            }

            return pool;
        }

        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var reg = await GetSmartContractRegistrationAsync(chainContext, address);
            var pool = GetPool(reg.CodeHash);

            if (!pool.TryTake(out var executive))
            {
                executive = await GetExecutiveAsync(reg);
            }

            return executive;
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
        
        public async Task PutExecutiveAsync(IExecutive executive)
        {
            if (_executivePools.TryGetValue(executive.ContractHash, out var pool))
            {
                pool.Add(executive);
            }

            await Task.CompletedTask;
        }

        public void Init(Hash blockHash,long blockHeight)
        {
            _initLibBlockHash = blockHash;
            _initLibBlockHeight = blockHeight;
        }

        private void ClearExecutives(IEnumerable<Hash> codeHashes)
        {
            foreach (var codeHash in codeHashes)
            {
                _executivePools.TryRemove(codeHash, out _);
            }
        }

        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,
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

        private SmartContractRegistrationCache GetSmartContractRegistrationCacheFromForkCache(IChainContext chainContext,Address address)
        {
            if (!_forkCache.TryGetValue(address, out var caches)) return null;
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

        private async Task<SmartContractRegistrationCache> GetSmartContractRegistrationCacheFromLibCache(IChainContext chainContext, Address address)
        {
            if (!_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var smartContractRegistrationCache))
            {
                //Use lib chain context to set lib cache. Genesis block need to execute with state cache
                var context = new ChainContext
                {
                    BlockHash = _initLibBlockHash,
                    BlockHeight = _initLibBlockHeight,
                    StateCache = chainContext.BlockHeight == 0 ? chainContext.StateCache : null
                };
                if (_deployedContractAddressProvider.CheckContractAddress(context, address))
                {
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
                    _addressSmartContractRegistrationMappingCache[address] = smartContractRegistrationCache;
                }
            }

            return smartContractRegistrationCache;
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(SmartContractRegistrationCache smartContractRegistrationCache, Address address, IStateCache stateCache = null)
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

        public void AddSmartContractRegistration(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            var smartContractRegistrationCache = new SmartContractRegistrationCache
            {
                Address = address,
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight,
                SmartContractRegistration = new SmartContractRegistration
                {
                    CodeHash = codeHash
                }
            };
            //Add genesis block registration cache to lib cache directly
            if (blockIndex.BlockHeight == 1)
            {
                _addressSmartContractRegistrationMappingCache.TryAdd(address, smartContractRegistrationCache);
                return;
            }

            if (!_forkCache.TryGetValue(address, out var caches))
            {
                caches = new List<SmartContractRegistrationCache>();
                _forkCache[address] = caches;
            }

            caches.Add(smartContractRegistrationCache);
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address)
        {
            IExecutive executiveZero = null;
            try
            {
                executiveZero = await GetExecutiveAsync(chainContext,_defaultContractZeroCodeProvider.ContractZeroAddress);
                return await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, address);
            }
            finally
            {
                if (executiveZero != null) await PutExecutiveAsync(executiveZero);
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
        
        private class SmartContractRegistrationCache
        {
            public Address Address { get; set; }
        
            public SmartContractRegistration SmartContractRegistration { get; set; }
        
            public Hash BlockHash { get; set; }
        
            public long BlockHeight { get; set; }
        }
    }
}