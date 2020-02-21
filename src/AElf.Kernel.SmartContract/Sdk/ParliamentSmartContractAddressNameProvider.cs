using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    //TODO: should not be here
    public class ParliamentSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Parliament");
        public Hash ContractName => Name;
    }
}