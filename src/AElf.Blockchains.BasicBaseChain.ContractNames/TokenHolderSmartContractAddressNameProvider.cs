using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.BasicBaseChain.ContractNames
{
    public class TokenHolderSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.TokenHolder");
        public Hash ContractName => Name;
    }
}