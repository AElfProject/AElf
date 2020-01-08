using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class AssociationSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Association");
        public Hash ContractName => Name;
    }
}