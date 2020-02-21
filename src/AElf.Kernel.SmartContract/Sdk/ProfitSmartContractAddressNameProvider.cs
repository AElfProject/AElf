using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    //TODO: move
    public class ProfitSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.Profit");
        public Hash ContractName => Name;
    }
}