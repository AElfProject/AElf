using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

        private readonly ConcurrentDictionary<Address, PoolWithId> _executivePools =
            new ConcurrentDictionary<Address, PoolWithId>();

        private readonly ConcurrentDictionary<Address, SmartContractRegistration>
            _addressSmartContractRegistrationMappingCache =
                new ConcurrentDictionary<Address, SmartContractRegistration>();

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());
        private readonly ConcurrentDictionary<Address, long> _contractInfoCache =
            new ConcurrentDictionary<Address, long>();

        private readonly IReadOnlyDictionary<Address, long> _readOnlyContractInfoCache;
        
        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        public SmartContractExecutiveService(
            ISmartContractRunnerContainer smartContractRunnerContainer, IBlockchainStateManager blockchainStateManager,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _blockchainStateManager = blockchainStateManager;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _readOnlyContractInfoCache = new ReadOnlyDictionary<Address, long>(_contractInfoCache);
        }

        public class PoolWithId : ConcurrentBag<IExecutive>
        {
            public Guid Id { get; set; }

            public PoolWithId()
            {
                Id = Guid.NewGuid();
            }
        }

        private PoolWithId GetPool(Address address)
        {
            if (!_executivePools.TryGetValue(address, out var pool))
            {
                pool = new PoolWithId();
                _executivePools[address] = pool;
                Logger.LogDebug($"Added new pool for {address.Value.ToBase64()}, pool id: {pool.Id}");
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
                Logger.LogDebug($"No executives in pool for {address.Value.ToBase64()} - pool id: {pool.Id}");
                
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
                        Logger.LogDebug($"Overriding executive {address.Value.ToBase64()} - pool id: {pool.Id}");
                        executive = await GetExecutiveAsync(address, reg);
                    }
                    
                    //add cache for zero, because GetSmartContractRegistrationAsync method do not add zero cache
                    _addressSmartContractRegistrationMappingCache.TryAdd(address, reg);

                }
            }
            else
            {
                Logger.LogDebug($"Taken an executive for {address.Value.ToBase64()} - pool id: {pool.Id} - {executive.AssemblyName()}");
            }

            return await GetExecutiveAsync(chainContext, address, executive);
        }

        private async Task<IExecutive> GetExecutiveAsync(Address address, SmartContractRegistration reg)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);

            // run smartcontract executive info and return executive
            Logger.LogDebug($"Creating executive for {address.Value.ToBase64()}");
            var executive = await runner.RunAsync(reg);

            var context =
                _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(context);
            return executive;
        }
        
        private List<WeakReference> _unloadableExecutives = new List<WeakReference>();

        public virtual async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var pool))
            {
                if (_addressSmartContractRegistrationMappingCache.TryGetValue(address, out var reg))
                {
                    if (reg.CodeHash == executive.ContractHash)
                    {
                        pool.Add(executive);
                        Logger.LogDebug($"PutExecutiveAsync - Put back executive to pool for {address.Value.ToBase64()} pool count {pool.Count}, pool id: {pool.Id}, name {executive.AssemblyName()}");
                    }
                }
            }
            else
            {
                Logger.LogDebug($"PutExecutiveAsync - Could not find executive pool for {address.Value.ToBase64()}");
                UnloadExecutive(executive, address);
            }

            await Task.CompletedTask;
        }

        private bool UnloadExecutive(IExecutive executive, Address address)
        {
            Logger.LogDebug($"UnloadExecutive - About to unload {executive.AssemblyName()}");
            
            WeakReference loadContext = executive.Unload();
            if (!loadContext.IsAlive)
            {
                Logger.LogDebug($"UnloadExecutive - Unloaded {address.Value.ToBase64()} -- {executive.AssemblyName()}");
                return true;
            }
            else
            {
                _unloadableExecutives.Add(loadContext);
                Logger.LogDebug($"UnloadExecutive - Could not unload {address.Value.ToBase64()} -- {executive.AssemblyName()}");
                return false;
            }
        }

        public void PrintUnloadedAssemblies()
        {
            Logger.LogDebug($"PrintUnloadedAssemblies - Starting unloading");
            
            foreach (var unloadableExecutive in _unloadableExecutives)
            {
                Logger.LogDebug($"PrintUnloadedAssemblies - weak ref: {unloadableExecutive.IsAlive}");
            }
        }

        public async Task SetContractInfoAsync(Address address, long blockHeight)
        {
            try
            {
                if (_executivePools.TryRemove(address, out var oldExecutives))
                {
                    Logger.LogDebug($"SetContractInfoAsync - Removed pool for the address {address.Value.ToBase64()} - pool id: {oldExecutives.Id} - count {oldExecutives.Count}");
                
                    foreach (var executive in oldExecutives.ToList())
                    {
                        UnloadExecutive(executive, address);
//                        if (executive.Unload())
//                            Logger.LogDebug($"SetContractInfoAsync - Unloaded {address.Value.ToBase64()} - {executive.AssemblyName()}");
//                        else
//                        {
//                            _unloadableExecutives.Add(executive);
//
//                            Logger.LogDebug(
//                                $"SetContractInfoAsync - executive not unloaded {address.Value.ToBase64()} - {executive.AssemblyName()}");
//                        }
                    }
                    
                    Logger.LogDebug($"SetContractInfoAsync - Unloaded executives for {address.Value.ToBase64()} - pool id: {oldExecutives.Id} ");
                }

                _addressSmartContractRegistrationMappingCache.TryRemove(address, out _);
            
                if (!_contractInfoCache.TryGetValue(address, out var height) || blockHeight > height)
                {
                    _contractInfoCache[address] = blockHeight;
                    var chainContractInfo = await _blockchainStateManager.GetChainContractInfoAsync();
                    chainContractInfo.ContractInfos[address.ToStorageKey()] = blockHeight;
                    await _blockchainStateManager.SetChainContractInfoAsync(chainContractInfo);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"error");
                Logger.LogError(e, $"error when removing pool for the address { address.Value.ToBase64() }");
                throw;
            }
        }

        public void ClearContractInfoCache(long blockHeight)
        {
            var addresses = _contractInfoCache.Keys;
            foreach (var address in addresses)
            {
                if (_contractInfoCache.TryGetValue(address, out var height) && blockHeight >= height)
                    _contractInfoCache.TryRemove(address, out _);
            }
        }

        public async Task InitContractInfoCacheAsync()
        {
            if (!_contractInfoCache.IsEmpty) return;
            
            var chainContractInfo = await _blockchainStateManager.GetChainContractInfoAsync();
            if (chainContractInfo.ContractInfos.IsNullOrEmpty()) return;
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainContractInfo.ContractInfos.RemoveAll(c => c.Value <= chainStateInfo.BlockHeight);
            await _blockchainStateManager.SetChainContractInfoAsync(chainContractInfo);
            foreach (var key in chainContractInfo.ContractInfos.Keys)
            {
                _contractInfoCache[AddressHelper.Base58StringToAddress(key)] = chainContractInfo.ContractInfos[key];
            }
        }

        public bool IsContractDeployOrUpdating(Address address)
        {
            return _contractInfoCache.TryGetValue(address, out _);
        }

        public IReadOnlyDictionary<Address, long> GetContractInfoCache()
        {
            return _readOnlyContractInfoCache;
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
            Logger.LogDebug($"[assembly] Getting the smart contract reg {address.Value.ToBase64()}.");

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
                    Logger.LogDebug($"[assembly] About to put back executive {address.Value.ToBase64()}.");
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
        
        private async Task<SmartContractRegistration> GetGetSmartContractRegistrationWithoutCacheAsync(IChainContext chainContext, Address address)
        {
            SmartContractRegistration reg;
            if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
            {
                reg = _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                if (chainContext.BlockHeight > Constants.GenesisBlockHeight)
                {
                    //if Height > GenesisBlockHeight, maybe there is a new zero contract, the current executive is from code,
                    //not from zero contract, so we need to load new zero contract from the old executive,
                    //and replace it
                    var executive = await GetExecutiveAsync(address, reg);
                    reg = await GetSmartContractRegistrationFromZeroAsync(executive, chainContext, address);
                }
            }
            else
            {
                reg = await GetSmartContractRegistrationFromZeroAsync(chainContext, address);
            }
            _addressSmartContractRegistrationMappingCache[address] = reg;

            return reg;
        }

        private async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address,
            IExecutive executive)
        {
            if (!_contractInfoCache.TryGetValue(address, out var height) || height == 1) 
                return executive;

            var smartContractRegistration = await GetGetSmartContractRegistrationWithoutCacheAsync(chainContext, address);
            if (smartContractRegistration.CodeHash == executive.ContractHash) return executive;
            executive = await GetExecutiveAsync(address, smartContractRegistration);
            return executive;
        }

        #endregion
    }
}