using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class TokenSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.Contracts.Token.TokenContract");
        public Hash ContractName => Name;
    }
}