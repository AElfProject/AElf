using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class TokenSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Token");

        public Hash ContractName => Name;
    }
}