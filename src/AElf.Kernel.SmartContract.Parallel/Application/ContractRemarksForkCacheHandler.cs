using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ContractRemarksForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly IContractRemarksManager _contractRemarksManager;

        public ContractRemarksForkCacheHandler(IContractRemarksManager contractRemarksManager)
        {
            _contractRemarksManager = contractRemarksManager;
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _contractRemarksManager.RemoveContractRemarksCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            AsyncHelper.RunSync(() => _contractRemarksManager.SetIrreversedCacheAsync(blockIndexes));
        }
    }
}