using AElf.Common;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressNameProvider 
    {
        Hash ContractName { get; }
    }
}