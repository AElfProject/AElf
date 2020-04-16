using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressProvider
    {
        Task<SmartContractAddress> GetSmartContractAddressAsync(IChainContext chainContext, Hash contractName);

        Task SetSmartContractAddressAsync(IBlockIndex blockIndex, Hash contractName, Address address);
    }

    public class SmartContractAddressProvider : BlockExecutedDataBaseProvider<SmartContractAddress>,ISmartContractAddressProvider, ISingletonDependency
    {
        public SmartContractAddressProvider(
            ICachedBlockchainExecutedDataService<SmartContractAddress> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }

        public Task<SmartContractAddress> GetSmartContractAddressAsync(IChainContext chainContext, Hash contractName)
        {
            var smartContractAddress = GetBlockExecutedData(chainContext, contractName);
            return Task.FromResult(smartContractAddress);
        }

        public async Task SetSmartContractAddressAsync(IBlockIndex blockIndex, Hash contractName,Address address)
        {
            await AddBlockExecutedDataAsync(blockIndex, contractName, new SmartContractAddress
            {
                Address = address,
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            });
        }

        protected override string GetBlockExecutedDataName()
        {
            return nameof(SmartContractAddress);
        }
    }
}