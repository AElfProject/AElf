using AElf.Common;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class TokenConverterSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractsNames.TokenConverter");
        public Hash ContractName => Name;
    }
}