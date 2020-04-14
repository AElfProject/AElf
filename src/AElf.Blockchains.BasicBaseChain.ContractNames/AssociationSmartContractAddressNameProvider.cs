using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.BasicBaseChain.ContractNames
{
    public class AssociationSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.ComputeFrom("AElf.ContractNames.Association");
        public Hash ContractName => Name;
    }
}