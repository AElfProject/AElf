using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractCodeHashProvider
    {
        Task<Hash> GetSmartContractCodeHashAsync(IChainContext chainContext, Address address);
        Task SetSmartContractCodeHashAsync(Hash blockHash, Address address, Hash codeHash);
    }

    public class SmartContractCodeHashProvider : BlockExecutedCacheProvider, ISmartContractCodeHashProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(SmartContractRegistration);

        private readonly IBlockchainStateService _blockchainStateService;

        public SmartContractCodeHashProvider(IBlockchainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
        }

        public async Task<Hash> GetSmartContractCodeHashAsync(IChainContext chainContext, Address address)
        {
            var key = GetBlockExecutedCacheKey(address);
            var registration =
                await _blockchainStateService.GetBlockExecutedDataAsync<SmartContractRegistration>(chainContext, key);
            return registration?.CodeHash;
        }

        public async Task SetSmartContractCodeHashAsync(Hash blockHash, Address address, Hash codeHash)
        {
            var key = GetBlockExecutedCacheKey(address);
            await _blockchainStateService.AddBlockExecutedDataAsync(blockHash, key, new SmartContractRegistration
            {
                CodeHash = codeHash
            });
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}