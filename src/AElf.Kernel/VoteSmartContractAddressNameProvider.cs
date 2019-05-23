using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class VoteSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Vote");
        public Hash ContractName => Name;
    }
}