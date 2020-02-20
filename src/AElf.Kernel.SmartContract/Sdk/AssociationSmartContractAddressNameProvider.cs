using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    //TODO: move
    public class AssociationSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Association");
        public Hash ContractName => Name;
    }
}