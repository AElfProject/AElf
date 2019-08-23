using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ResourceSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Resource");
        public Hash ContractName => Name;
    }
}