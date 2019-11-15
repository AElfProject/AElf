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

        public void RemoveForkCache(List<Hash> blockHashes)
        {
            _deployedContractAddressProvider.RemoveForkCache(blockHashes);
        }

        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            _deployedContractAddressProvider.SetIrreversedCache(blockHashes);
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            _deployedContractAddressProvider.SetIrreversedCache(blockHash);
        }
    }
}