using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Proposal
{
    public class ParliamentSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = HashHelper.ComputeFromString("AElf.ContractNames.Parliament");
        public Hash ContractName => Name;
    }
}