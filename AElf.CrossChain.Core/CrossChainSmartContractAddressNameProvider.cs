using AElf.Common;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString(typeof(CrossChainContract).FullName);

        public Hash ContractName => Name;
    }
}