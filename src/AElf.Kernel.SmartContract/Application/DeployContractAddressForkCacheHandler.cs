using System.Collections.Generic;
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

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _deployedContractAddressProvider.RemoveForkCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            _deployedContractAddressProvider.SetIrreversedCache(blockIndexes);
        }
    }
}