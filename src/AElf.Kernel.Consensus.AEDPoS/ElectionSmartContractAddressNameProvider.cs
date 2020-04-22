using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public class ElectionSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = HashHelper.ComputeFromString("AElf.ContractNames.Election");

        public Hash ContractName => Name;
    }
}