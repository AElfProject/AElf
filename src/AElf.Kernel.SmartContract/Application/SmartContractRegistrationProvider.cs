using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractRegistrationProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address);

        bool TryGetCachedSmartContractRegistrationAsync(Address address,
            out SmartContractRegistration smartContractRegistration);

        Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration);

        Task SyncRegistrationCacheFromStateAsync(IBlockIndex blockIndex);
    }

    public class SmartContractRegistrationProvider : BlockExecutedCacheProvider, ISmartContractRegistrationProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(SmartContractRegistration);
        
        private readonly ConcurrentDictionary<Address, SmartContractRegistration> _smartContractRegistrationCache =
            new ConcurrentDictionary<Address, SmartContractRegistration>();
        private readonly ConcurrentDictionary<Address, long> _smartContractChangeHeightMappings =
            new ConcurrentDictionary<Address, long>();

        private readonly IBlockchainExecutedDataService _blockchainExecutedDataService;

        public SmartContractRegistrationProvider(IBlockchainExecutedDataService blockchainExecutedDataService)
        {
            _blockchainExecutedDataService = blockchainExecutedDataService;
        }


        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address)
        {
            if (!_smartContractRegistrationCache.TryGetValue(address, out var smartContractRegistration))
            {
                smartContractRegistration = await GetRegistrationFromStateAsync(chainContext, address);
                if (smartContractRegistration != null)
                    _smartContractRegistrationCache[address] = smartContractRegistration;
            }
            else if (_smartContractChangeHeightMappings.TryGetValue(address, out _))
            {
                return await GetRegistrationFromStateAsync(chainContext, address);
            }

            return smartContractRegistration;
        }

        public bool TryGetCachedSmartContractRegistrationAsync(Address address,
            out SmartContractRegistration smartContractRegistration)
        {
            return _smartContractRegistrationCache.TryGetValue(address, out smartContractRegistration);
        }

        private async Task<SmartContractRegistration> GetRegistrationFromStateAsync(IChainContext chainContext,
            Address address)
        {
            var key = GetBlockExecutedCacheKey(address);
            var smartContractRegistration =
                await _blockchainExecutedDataService.GetBlockExecutedDataAsync<SmartContractRegistration>(chainContext, key);
            return smartContractRegistration;
        }

        public async Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration)
        {
            var key = GetBlockExecutedCacheKey(address);
            await _blockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key, smartContractRegistration);
            _smartContractRegistrationCache[address] = smartContractRegistration;
            
            if (blockIndex.BlockHeight <= Constants.GenesisBlockHeight) return;
            if (!_smartContractChangeHeightMappings.TryGetValue(address, out var height) ||
                height < blockIndex.BlockHeight)
                _smartContractChangeHeightMappings[address] = blockIndex.BlockHeight;
        }

        public async Task SyncRegistrationCacheFromStateAsync(IBlockIndex blockIndex)
        {
            var removeAddresses = (from contractInfo in _smartContractChangeHeightMappings
                where contractInfo.Value <= blockIndex.BlockHeight
                select contractInfo.Key).ToList();

            foreach (var address in removeAddresses)
            {
                var smartContractRegistration = await GetRegistrationFromStateAsync(new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                }, address);
                if (smartContractRegistration != null)
                    _smartContractRegistrationCache[address] = smartContractRegistration;
                _smartContractChangeHeightMappings.TryRemove(address, out _);
            }
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}