using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface INonparallelContractCodeProvider
    {
        Task<NonparallelContractCode> GetNonparallelContractCodeAsync(IChainContext chainContext, Address address);

        Task SetNonparallelContractCodeAsync(Hash blockHash,
            IDictionary<Address, NonparallelContractCode> nonparallelContractCodes);
    }

    public class NonparallelContractCodeProvider : BlockExecutedDataProvider, INonparallelContractCodeProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(NonparallelContractCode);

        private readonly IBlockchainExecutedDataService _blockchainExecutedDataService;

        public NonparallelContractCodeProvider(IBlockchainExecutedDataService blockchainExecutedDataService)
        {
            _blockchainExecutedDataService = blockchainExecutedDataService;
        }


        public async Task<NonparallelContractCode> GetNonparallelContractCodeAsync(IChainContext chainContext, Address address)
        {
            var key = GetBlockExecutedDataKey(address);
            return await _blockchainExecutedDataService.GetBlockExecutedDataAsync<NonparallelContractCode>(chainContext, key);
        }

        public async Task SetNonparallelContractCodeAsync(Hash blockHash, IDictionary<Address, NonparallelContractCode> nonparallelContractCodes)
        {
            var dic = nonparallelContractCodes.ToDictionary(pair => GetBlockExecutedDataKey(pair.Key),
                pair => pair.Value);
            await _blockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, dic);
        }
        
        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}