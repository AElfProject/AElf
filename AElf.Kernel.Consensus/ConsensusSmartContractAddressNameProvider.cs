using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    public class ConsensusSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.Kernel.Consensus.ConsensusContract");

        public Hash ContractName => Name;
    }
}