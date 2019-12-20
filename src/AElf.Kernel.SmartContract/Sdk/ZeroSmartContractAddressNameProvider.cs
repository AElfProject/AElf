using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ZeroSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.Empty;
        public Hash ContractName => Name;
    }
}