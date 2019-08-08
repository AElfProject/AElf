using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveService : ISmartContractExecutiveService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

        private readonly ConcurrentDictionary<Address, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>();

        private readonly ConcurrentDictionary<Address, SmartContractRegistration>
            _addressSmartContractRegistrationMappingCache =
                new ConcurrentDictionary<Address, SmartContractRegistration>();

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());
        private readonly ConcurrentDictionary<Address, long> _contractInfoCache =
            new ConcurrentDictionary<Address, long>();

        public SmartContractExecutiveService(
            ISmartContractRunnerContainer smartContractRunnerContainer, IBlockchainStateManager blockchainStateManager,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _blockchainStateManager = blockchainStateManager;
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

            return await GetExecutiveAsync(chainContext, address, executive);
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

        public async Task SetContractInfoAsync(Address address, long blockHeight)
        {
            if (!_contractInfoCache.TryGetValue(address, out var height) || blockHeight > height)
            {
                _contractInfoCache[address] = blockHeight;
                var chainContractInfo = await _blockchainStateManager.GetChainContractInfoAsync();
                chainContractInfo.ContractInfos[address.ToStorageKey()] = blockHeight;
                await _blockchainStateManager.SetChainContractInfoAsync(chainContractInfo);
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
            if (!_contractInfoCache.TryGetValue(address, out _) || chainContext.BlockHeight == 0) return executive;

            var key = string.Join("/",
                _defaultContractZeroCodeProvider.ContractZeroAddress.GetFormatted(),
                "ContractInfos", address.ToString());
            var byteString =
                await _blockchainStateManager.GetStateAsync(key, chainContext.BlockHeight, chainContext.BlockHash);
            if (byteString == null)
                throw new InvalidOperationException("failed to find registration from zero contract");
            var codeHash = ContractInfo.Parser.ParseFrom(byteString).CodeHash;
            if (codeHash == executive.ContractHash) return executive;
            var smartContractRegistration =
                await GetGetSmartContractRegistrationWithoutCacheAsync(chainContext, address);
            executive = await GetExecutiveAsync(address, smartContractRegistration);

            return executive;
        }

        #endregion
    }
}