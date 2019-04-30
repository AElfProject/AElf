using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ResourceSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractsNames.Resource");
        public Hash ContractName => Name;
    }
}