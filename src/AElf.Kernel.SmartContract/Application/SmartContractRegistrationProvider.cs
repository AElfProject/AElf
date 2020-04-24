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

    public class SmartContractRegistrationProvider : BlockExecutedDataBaseProvider<SmartContractRegistration>, ISmartContractRegistrationProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(SmartContractRegistration);

        public SmartContractRegistrationProvider(
            ICachedBlockchainExecutedDataService<SmartContractRegistration> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {

        }

        public Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address)
        {
            var smartContractRegistration = GetBlockExecutedData(chainContext, address);
            return Task.FromResult(smartContractRegistration);
        }

        public async Task SetSmartContractRegistrationAsync(IBlockIndex blockIndex, Address address,
            SmartContractRegistration smartContractRegistration)
        {
            await AddBlockExecutedDataAsync(blockIndex, address, smartContractRegistration);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}