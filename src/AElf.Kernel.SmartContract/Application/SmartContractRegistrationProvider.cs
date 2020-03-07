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

        private readonly IBlockchainExecutedDataService _blockchainExecutedDataService;

        public SmartContractRegistrationProvider(IBlockchainExecutedDataService blockchainExecutedDataService)
        {
            _blockchainExecutedDataService = blockchainExecutedDataService;
        }


        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address)
        {
            var key = GetBlockExecutedDataKey(address);
            var smartContractRegistration =
                await _blockchainExecutedDataService.GetBlockExecutedDataAsync<SmartContractRegistration>(chainContext, key);
            return smartContractRegistration;
        }

        public async Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration)
        {
            var key = GetBlockExecutedDataKey(address);
            await _blockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex.BlockHash, key,
                smartContractRegistration);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}