using AElf.Kernel.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract
{
    public class TestSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = HashHelper.ComputeFrom("AElf.Contracts.Test");
        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}