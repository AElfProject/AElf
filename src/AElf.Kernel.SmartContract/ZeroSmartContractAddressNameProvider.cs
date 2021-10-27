using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ZeroSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.Empty;
        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}