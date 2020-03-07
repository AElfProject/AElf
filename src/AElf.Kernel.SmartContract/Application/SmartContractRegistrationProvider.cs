using System.Collections.Concurrent;
using System.Threading.Tasks;
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
    }

    public class SmartContractRegistrationProvider : BlockExecutedCacheProvider, ISmartContractRegistrationProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(SmartContractRegistration);
        
        private readonly ConcurrentDictionary<Address, SmartContractRegistration> _smartContractRegistrationCache =
            new ConcurrentDictionary<Address, SmartContractRegistration>();

        private readonly IBlockchainStateService _blockchainStateService;

        public SmartContractRegistrationProvider(IBlockchainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
        }

        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address)
        {
            if (!_smartContractRegistrationCache.TryGetValue(address, out var smartContractRegistration))
            {
                smartContractRegistration = await GetRegistrationFromStateAsync(chainContext, address);
                if (smartContractRegistration != null)
                    _smartContractRegistrationCache[address] = smartContractRegistration;
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
                await _blockchainStateService.GetBlockExecutedDataAsync<SmartContractRegistration>(chainContext, key);
            return smartContractRegistration;
        }

        public async Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration)
        {
            var key = GetBlockExecutedCacheKey(address);
            await _blockchainStateService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key, smartContractRegistration);
            _smartContractRegistrationCache[address] = smartContractRegistration;
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}