using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class ReferendumAuthSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Referendum");
        public Hash ContractName => Name;
    }
}