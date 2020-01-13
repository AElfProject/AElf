using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Extensions;
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

        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly IExecutiveService _executiveService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public ILogger<SmartContractExecutiveService> Logger { get; set; }

        public SmartContractExecutiveService(IDeployedContractAddressProvider deployedContractAddressProvider,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider, 
            IBlockchainService blockchainService, 
            IExecutiveService executiveService, 
            ISmartContractRegistrationService smartContractRegistrationService)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _blockchainService = blockchainService;
            _executiveService = executiveService;
            _smartContractRegistrationService = smartContractRegistrationService;

            Logger = NullLogger<SmartContractExecutiveService>.Instance;
        }

        public async Task<IExecutive> GetExecutiveAsync(IChainContext chainContext, Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var reg = await GetSmartContractRegistrationAsync(chainContext, address);
            var pool = _executiveService.GetPool(address, reg.CodeHash);

            if (!pool.TryTake(out var executive))
            {
                executive = await _executiveService.GetExecutiveAsync(reg);
            }

            return executive;
        }

        public async Task<IExecutive> GetHistoryExecutiveAsync(IChainContext chainContext, Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (chainContext.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, chainContext.BlockHeight,
                    chainContext.BlockHash);
                if(blockHash != chainContext.BlockHash) return null;
            }

            var smartContractCodeHistory =
                await _smartContractRegistrationService.GetSmartContractCodeHistoryAsync(address);
            if (smartContractCodeHistory == null) return null;
            var smartContractCodes = smartContractCodeHistory.Codes
                .Where(c => c.BlockHeight <= chainContext.BlockHeight).OrderByDescending(c => c.BlockHeight).ToList();
            
            foreach (var smartContractCode in smartContractCodes)
            {
                var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, smartContractCode.BlockHeight,
                    chainContext.BlockHash);
                if (blockHash != smartContractCode.BlockHash)
                {
                    continue;
                }
                var pool = _executiveService.GetPool(address, smartContractCode.CodeHash);
                if (pool.TryTake(out var executive)) return executive;
                var smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(
                    new ChainContext
                    {
                        BlockHash = smartContractCode.BlockHash,
                        BlockHeight = smartContractCode.BlockHeight
                    },
                    address,
                    smartContractCode.CodeHash);
                executive = await _executiveService.GetExecutiveAsync(smartContractRegistration);

                return executive;
            }
            return null;
        }

        public async Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            _executiveService.PutExecutive(address,executive);
            await Task.CompletedTask;
        }

        public void CleanIdleExecutive()
        {
            _executiveService.CleanIdleExecutive();
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,
            Address address)
        {
            var registrationCache =
                _smartContractRegistrationService.GetSmartContractRegistrationCacheFromForkCache(chainContext, address);

            if (registrationCache != null)
            {
                return await GetSmartContractRegistrationAsync(registrationCache, address);
            }

            registrationCache = await GetSmartContractRegistrationCacheFromLibCache(chainContext, address);
            return await GetSmartContractRegistrationAsync(registrationCache, address, chainContext.StateCache);
        }

        private async Task<SmartContractRegistrationCache> GetSmartContractRegistrationCacheFromLibCache(
            IChainContext chainContext, Address address)
        {
            if (_smartContractRegistrationService.TryGetSmartContractRegistrationLibCache(address,
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
            var smartContractRegistration = await GetSmartContractRegistrationFromZeroAsync(context, address);
            smartContractRegistrationCache = new SmartContractRegistrationCache
            {
                SmartContractRegistration = smartContractRegistration,
                BlockHash = context.BlockHash,
                BlockHeight = context.BlockHeight,
                Address = address
            };
            _smartContractRegistrationService.SetSmartContractRegistrationLibCache(address, smartContractRegistrationCache);
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
                    if (chainContext.BlockHeight <= Constants.GenesisBlockHeight)
                        return _defaultContractZeroCodeProvider.DefaultContractZeroRegistration;
                    executiveZero =
                        await _executiveService.GetExecutiveAsync(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration);
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
            var transaction = new Transaction
            {
                From = FromAddress,
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "GetSmartContractRegistrationByAddress",
                Params = address.ToByteString()
            };

            return await executiveZero.GetSmartContractRegistrationFromZeroAsync(chainContext, transaction);
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IChainContext chainContext, Address address, Hash codeHash)
        {
            IExecutive executiveZero = null;
            try
            {
                if (address == _defaultContractZeroCodeProvider.ContractZeroAddress)
                {
                    executiveZero =
                        await _executiveService.GetExecutiveAsync(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration);
                }
                else
                {
                    executiveZero =
                        await GetHistoryExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
                }

                return await GetSmartContractRegistrationFromZeroAsync(executiveZero, chainContext, codeHash);
            }
            finally
            {
                if (executiveZero != null)
                    await PutExecutiveAsync(_defaultContractZeroCodeProvider.ContractZeroAddress, executiveZero);
            }
        }

        private async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            IExecutive executiveZero, IChainContext chainContext, Hash codeHash)
        {
            var transaction = new Transaction
            {
                From = FromAddress,
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "GetSmartContractRegistration",
                Params = codeHash.ToByteString()
            };
            return await executiveZero.GetSmartContractRegistrationFromZeroAsync(chainContext, transaction);
        }
    }
}