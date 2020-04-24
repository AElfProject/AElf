using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class AssociationSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = HashHelper.ComputeFromString("AElf.ContractNames.Association");
        public Hash ContractName => Name;
    }
}