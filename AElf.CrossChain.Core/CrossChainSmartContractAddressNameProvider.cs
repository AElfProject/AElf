using AElf.Common;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.CrossChain");

        public Hash ContractName => Name;
    }
}