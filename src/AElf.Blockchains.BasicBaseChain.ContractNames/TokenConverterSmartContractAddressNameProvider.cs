using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.BasicBaseChain.ContractNames
{
    public class TokenConverterSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = HashHelper.ComputeFromString("AElf.ContractNames.TokenConverter");
        public Hash ContractName => Name;
    }
}