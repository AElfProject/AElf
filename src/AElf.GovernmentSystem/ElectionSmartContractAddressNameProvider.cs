using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class ElectionSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = HashHelper.ComputeFromString("AElf.ContractNames.Election");

        public Hash ContractName => Name;
    }
}