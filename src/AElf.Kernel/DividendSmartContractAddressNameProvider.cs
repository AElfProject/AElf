using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract
{
    public class DividendSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Dividend");
        public Hash ContractName => Name;
    }
}