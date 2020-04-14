using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.BasicBaseChain.ContractNames
{
    public class ResourceSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.ComputeFrom("AElf.ContractNames.Resource");
        public Hash ContractName => Name;
    }
}