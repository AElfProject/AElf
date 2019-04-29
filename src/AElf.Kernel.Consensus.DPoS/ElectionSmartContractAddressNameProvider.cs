using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ElectionSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = Hash.FromString("AElf.ContractNames.Election");

        public Hash ContractName => Name;
    }
}