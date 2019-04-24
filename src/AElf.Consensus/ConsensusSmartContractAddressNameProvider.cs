using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ConsensusSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = Hash.FromString("AElf.ContractNames.Consensus");

        public Hash ContractName => Name;
    }
}