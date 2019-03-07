using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    public class ConsensusSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString(typeof(ConsensusContract).FullName);

        public Hash ContractName => Name;
    }
}