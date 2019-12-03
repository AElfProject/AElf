using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class TokenConverterSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.TokenConverter");
        public Hash ContractName => Name;
    }
}