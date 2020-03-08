using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractRegistrationProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address);

        Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration);
    }

    public class SmartContractRegistrationProvider : BlockExecutedDataProvider, ISmartContractRegistrationProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(SmartContractRegistration);

        private readonly ICachedBlockchainExecutedDataService<SmartContractRegistration>
            _cachedBlockchainExecutedDataService;

        public SmartContractRegistrationProvider(
            ICachedBlockchainExecutedDataService<SmartContractRegistration> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }


        public Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address)
        {
            var key = GetBlockExecutedDataKey(address);
            var smartContractRegistration =
                _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, key);
            return Task.FromResult(smartContractRegistration);
        }

        public async Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration)
        {
            var key = GetBlockExecutedDataKey(address);
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key,
                smartContractRegistration);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}