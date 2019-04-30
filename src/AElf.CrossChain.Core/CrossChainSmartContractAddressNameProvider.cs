using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.CrossChain");

        public Hash ContractName => Name;
    }
}