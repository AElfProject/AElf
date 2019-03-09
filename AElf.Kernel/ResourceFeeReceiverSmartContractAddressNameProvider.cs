using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract
{
    public class ResourceFeeReceiverSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash Name = Hash.FromString("AElf.Contracts.Resource.FeeReceiver.FeeReceiverContract");
        public Hash ContractName => Name;
    }
}