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

        Task SetNonparallelContractCodeAsync(IBlockIndex blockIndex,
            IDictionary<Address, NonparallelContractCode> nonparallelContractCodes);
    }

    public class NonparallelContractCodeProvider : BlockExecutedDataBaseProvider<NonparallelContractCode>, INonparallelContractCodeProvider,
        ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(NonparallelContractCode);

        public NonparallelContractCodeProvider(
            ICachedBlockchainExecutedDataService<NonparallelContractCode> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }


        public Task<NonparallelContractCode> GetNonparallelContractCodeAsync(IChainContext chainContext, Address address)
        {
            var nonparallelContractCode = GetBlockExecutedData(chainContext, address);
            return Task.FromResult(nonparallelContractCode);
        }

        public async Task SetNonparallelContractCodeAsync(IBlockIndex blockIndex,
            IDictionary<Address, NonparallelContractCode> nonparallelContractCodes)
        {
            await AddBlockExecutedDataAsync(blockIndex, nonparallelContractCodes);
        }
        
        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}