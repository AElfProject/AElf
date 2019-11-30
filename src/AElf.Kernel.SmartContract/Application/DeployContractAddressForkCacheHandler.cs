using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class DeployContractAddressForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;

        public DeployContractAddressForkCacheHandler(IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _deployedContractAddressProvider.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _deployedContractAddressProvider.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}