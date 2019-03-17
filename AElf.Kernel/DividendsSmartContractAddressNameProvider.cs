using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract
{
    public class DividendsSmartContractAddressNameProvider : ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static readonly Hash     Name = Hash.FromString("AElf.Contracts.Dividends.DividendsContract");
        public Hash ContractName => Name;
    }
}