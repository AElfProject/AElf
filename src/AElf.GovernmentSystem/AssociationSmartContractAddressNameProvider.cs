using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class AssociationSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = HashHelper.ComputeFrom("AElf.ContractNames.Association");
        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}