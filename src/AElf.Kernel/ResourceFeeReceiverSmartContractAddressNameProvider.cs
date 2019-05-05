using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class ResourceFeeReceiverSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.FeeReceiver");
        public Hash ContractName => Name;
    }
}