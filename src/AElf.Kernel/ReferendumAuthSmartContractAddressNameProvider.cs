using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class ReferendumAuthSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.ReferendumAuth");
        public Hash ContractName => Name;
    }
}