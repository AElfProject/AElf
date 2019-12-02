using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class AssociationAuthSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.AssociationAuth");
        public Hash ContractName => Name;
    }
}