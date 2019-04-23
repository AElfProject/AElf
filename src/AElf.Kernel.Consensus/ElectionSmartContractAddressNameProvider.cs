using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    public class ElectionSmartContractAddressNameProvider: ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = Hash.FromString("AElf.ContractNames.Election");

        public Hash ContractName => Name;
    }
}