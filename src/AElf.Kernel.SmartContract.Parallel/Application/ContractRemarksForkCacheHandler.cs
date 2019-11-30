using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ContractRemarksForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly IContractRemarksService _contractRemarksService;

        public ContractRemarksForkCacheHandler(IContractRemarksService contractRemarksService)
        {
            _contractRemarksService = contractRemarksService;
        }

        public async Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            await _contractRemarksService.RemoveContractRemarksCacheAsync(blockIndexes);
        }

        public async Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            await _contractRemarksService.SetIrreversedCacheAsync(blockIndexes);
        }
    }
}