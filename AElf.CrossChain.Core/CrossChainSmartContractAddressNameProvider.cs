using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name =
            Hash.FromString("AElf.Contracts.CrossChain.CrossChainContract");

        public Hash ContractName => Name;
    }
}