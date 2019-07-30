using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
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
        private readonly ConcurrentDictionary<Address, List<UpdateContractInfo>> _updateContractInfoCache =
            new ConcurrentDictionary<Address, List<UpdateContractInfo>>();
        private readonly ConcurrentDictionary<Address, Dictionary<Hash,Hash>> _linkedBlocksCache =
            new ConcurrentDictionary<Address, Dictionary<Hash,Hash>>();

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

        public void SetUpdateContractInfo(Address address, Hash codeHash, long blockHeight, Hash previousBlockHash)
        {
            if (!_updateContractInfoCache.TryGetValue(address, out var updateContractInfos))
            {
                updateContractInfos = new List<UpdateContractInfo>();
                _updateContractInfoCache[address] = updateContractInfos;
            }
            updateContractInfos.Add(new UpdateContractInfo
                {CodeHash = codeHash, BlockHeight = blockHeight, PrevBlockHash = previousBlockHash});
        }

        public void ClearUpdateContractInfo(long blockHeight)
        {
            var addresses = _updateContractInfoCache.Keys;
            foreach (var address in addresses)
            {
                if (!_updateContractInfoCache.TryGetValue(address, out var updateContractInfos)) continue;
                updateContractInfos.RemoveAll(info => info.BlockHeight <= blockHeight);
                if (updateContractInfos.Count != 0) continue;
                _updateContractInfoCache.TryRemove(address,out _);
                _linkedBlocksCache.TryRemove(address, out _);
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
            if (!_updateContractInfoCache.TryGetValue(address, out var updateContractInfos)) return executive;

            var minUpdateHeight = updateContractInfos.Select(c => c.BlockHeight).Min();
            var blockIndex = new BlockIndex(chainContext.BlockHash, chainContext.BlockHeight);

            UpdateContractInfo updateContractInfo = null;
            while (blockIndex.Height >= minUpdateHeight && updateContractInfo == null)
            {
                updateContractInfo = GetUpdateContractInfoByLinkedBlocks(blockIndex, address);
                if (updateContractInfo != null) break;
                updateContractInfo = await GetUpdateContractInfoByBlockStateSetAsync(blockIndex, address);
                blockIndex.Height--;
            }

            if (updateContractInfo != null && updateContractInfo.CodeHash != executive.ContractHash ||
                updateContractInfo == null && updateContractInfos.Any(c => c.CodeHash == executive.ContractHash))
            {
                var smartContractRegistration =
                    await GetGetSmartContractRegistrationWithoutCacheAsync(chainContext, address);
                executive = await GetExecutiveAsync(address, smartContractRegistration);
            }

            return executive;
        }

        private Dictionary<Hash, Hash> GetLinkedBlocks(Address address)
        {
            if (!_linkedBlocksCache.TryGetValue(address, out var linkedBlocks))
            {
                linkedBlocks = new Dictionary<Hash, Hash>();
                _linkedBlocksCache[address] = linkedBlocks;
            }

            return linkedBlocks;
        }

        private UpdateContractInfo GetUpdateContractInfoByLinkedBlocks(BlockIndex blockIndex, Address address)
        {
            var linkedBlocks = GetLinkedBlocks(address);
            if (!linkedBlocks.TryGetValue(blockIndex.Hash, out var blockHash)) return null;
            var updateContractInfos = _updateContractInfoCache[address];
            var updateContractInfo = updateContractInfos.FirstOrDefault(c => c.BlockHash == blockIndex.Hash);
            blockIndex.Hash = blockHash;
            return updateContractInfo;
        }

        private async Task<UpdateContractInfo> GetUpdateContractInfoByBlockStateSetAsync(BlockIndex blockIndex,Address address)
        {
            var updateContractInfos = _updateContractInfoCache[address];
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(blockIndex.Hash);
            blockIndex.Hash = blockStateSet.PreviousHash;
            if (updateContractInfos.All(c => c.BlockHeight != blockIndex.Height)) return null;
            
            var key = string.Join("/",
                _defaultContractZeroCodeProvider.ContractZeroAddress.GetFormatted(),
                "ContractInfos", address.ToString());
            if (!blockStateSet.Changes.TryGetValue(key, out var byteString)) return null;
            
            var codeHash = ContractInfo.Parser.ParseFrom(byteString).CodeHash;
            var updateContractInfo = updateContractInfos.FirstOrDefault(c =>
                c.CodeHash == codeHash && c.BlockHeight == blockStateSet.BlockHeight &&
                c.PrevBlockHash == blockStateSet.PreviousHash && c.BlockHash == null);
            if (updateContractInfo == null) return null;
            updateContractInfo.BlockHash = blockStateSet.BlockHash;
            return updateContractInfo;
        }

        #endregion

        class UpdateContractInfo
        {
            public long BlockHeight { get; set; }
            public Hash PrevBlockHash { get; set; }
            public Hash CodeHash { get; set; }
            public Hash BlockHash { get; set; }
        }
    }
}