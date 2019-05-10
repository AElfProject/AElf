using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AElfConsensus
{
    public class MinersCountProviderSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = Hash.FromString("AElf.ContractNames.MinersCountProvider");

        public Hash ContractName => Name;
    }
}