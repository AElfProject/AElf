using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class DividendSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Dividend");
        public Hash ContractName => Name;
    }
}