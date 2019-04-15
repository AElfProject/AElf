using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Vote
{
    public class VoteSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Vote");

        public Hash ContractName => Name;
    }
}