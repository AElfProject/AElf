using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    public class ConsensusSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = HashHelper.ComputeFrom("AElf.ContractNames.Consensus");

        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}