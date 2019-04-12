using AElf.Common;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class ResourceFeeReceiverSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.ContractNames.FeeReceiver");
        public Hash ContractName => Name;
    }
}