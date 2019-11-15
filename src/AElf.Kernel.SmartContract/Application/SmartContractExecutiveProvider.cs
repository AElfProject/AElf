using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address);

        Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address);

        Task PutExecutiveAsync(Address address, IExecutive executive);

        void Init(Hash blockHash, long blockHeight);

        void AddSmartContractRegistration(IBlockIndex blockIndex, Address address, Hash codeHash);

//        bool IsContractDeployOrUpdating(IChainContext chainContext, Address address);

        void RemoveForkCache(List<Hash> blockHashes);

        void SetIrreversedCache(List<Hash> blockHashes);
        void SetIrreversedCache(Hash blockHash);
    }
    
    
    public class SmartContractExecutiveProvider: ISmartContractExecutiveProvider, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;

        private readonly ConcurrentDictionary<Address, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>();

        private readonly ConcurrentDictionary<Address, SmartContractRegistrationCache>
            _addressSmartContractRegistrationMappingCache =
                new ConcurrentDictionary<Address, SmartContractRegistrationCache>();
        
        private readonly ConcurrentDictionary<Hash, List<SmartContractRegistrationCache>> _forkCache =
            new ConcurrentDictionary<Hash, List<SmartContractRegistrationCache>>();

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());
        private Hash _initLibBlockHash = Hash.Empty;
        private long _initLibBlockHeight;

        public ILogger<SmartContractExecutiveProvider> Logger { get; set; }
 
        public SmartContractExecutiveProvider(
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService, 
            IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _deployedContractAddressProvider = deployedContractAddressProvider;

            Logger = NullLogger<SmartContractExecutiveProvider>.Instance;
        }

        private ConcurrentBag<IExecutive> GetPool(Address address)
        {
            if (!_executivePools.TryGetValue(address, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[address] = pool;
            }

            //Logger.LogInformation($"Executive: Address= {address}, Count = {pool?.Count ?? 0}");
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
                executive = await GetExecutiveAsync(reg);
            }

            return executive;
        }
        
        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(
            IChainContext chainContext, Address address)
        {
            if (!_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var smartContractRegistrationCache))
            {
                if (_deployedContractAddressProvider.CheckContractAddress(address))
                {
                    var context = new ChainContext
                    {
                        BlockHash = _initLibBlockHash,
                        BlockHeight = _initLibBlockHeight,
                        StateCache = chainContext.BlockHeight == 0 ? chainContext.StateCache : null
                    };
                    SmartContractRegistration smartContractRegistration;
                    if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
                    {
                        smartContractRegistration =
                            _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
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

            return await GetSmartContractRegistrationAsync(smartContractRegistrationCache);
        }

        public virtual async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var pool))
            {
                if (_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var cache))
                {
                    if (cache.SmartContractRegistration.CodeHash == executive.ContractHash)
                    {
                        pool.Add(executive);
                    }
                }
            }

            await Task.CompletedTask;
        }
        
        public void Init(Hash blockHash,long blockHeight)
        {
            _initLibBlockHash = blockHash;
            _initLibBlockHeight = blockHeight;
        }
        
        public void AddSmartContractRegistration(IBlockIndex blockIndex,Address address,Hash codeHash)
        {
            if (!_forkCache.TryGetValue(blockIndex.BlockHash, out var caches))
            {
                caches = new List<SmartContractRegistrationCache>();
                _forkCache[blockIndex.BlockHash] = caches;
            }

            var cache = caches.FirstOrDefault(c =>
                c.BlockHash == blockIndex.BlockHash && c.SmartContractRegistration.CodeHash == codeHash);
            if (cache != null) return;
            cache = new SmartContractRegistrationCache
            {
                Address = address,
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight,
                SmartContractRegistration = new SmartContractRegistration
                {
                    CodeHash = codeHash
                }
            };
            if (blockIndex.BlockHeight == 1)
            {
                if (_addressSmartContractRegistrationMappingCache.TryGetValue(address, out _)) return;
                _addressSmartContractRegistrationMappingCache[address] = new SmartContractRegistrationCache
                {
                    Address = address,
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight,
                    SmartContractRegistration = new SmartContractRegistration
                    {
                        CodeHash = codeHash
                    }
                };
                return;
            }
            caches.Add(cache);
        }
        
        public void RemoveForkCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                if(!_forkCache.TryGetValue(blockHash, out _)) continue;
                _forkCache.TryRemove(blockHash, out _);
            }
        }
        
        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                SetIrreversedCache(blockHash);
            } 
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            if(!_forkCache.TryGetValue(blockHash, out var caches)) return;
            foreach (var cache in caches)
            {
                if (_addressSmartContractRegistrationMappingCache.TryGetValue(cache.Address,
                        out var smartContractRegistrationCache) &&
                    smartContractRegistrationCache.SmartContractRegistration.CodeHash ==
                    cache.SmartContractRegistration.CodeHash)
                    continue;
                _addressSmartContractRegistrationMappingCache[cache.Address] = cache;
                _executivePools.TryRemove(cache.Address, out _);
            }
            _forkCache.TryRemove(blockHash, out _);
        }

        #region private methods
        
        private async Task<IExecutive> GetExecutiveAsync(SmartContractRegistration reg)
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
        
        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(SmartContractRegistrationCache smartContractRegistrationCache)
        {
            if (smartContractRegistrationCache == null)
                throw new SmartContractFindRegistrationException("failed to find registration");
                
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
        
        #endregion
    }
    
    public class SmartContractRegistrationCache
    {
        public Address Address { get; set; }
        
        public SmartContractRegistration SmartContractRegistration { get; set; }
        
        public Hash BlockHash { get; set; }
        
        public long BlockHeight { get; set; }
    }
}