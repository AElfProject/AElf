using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract
{
    public class DividendsSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash     Name = Hash.FromString("AElf.ContractNames.Dividends");
        public Hash ContractName => Name;
    }
}