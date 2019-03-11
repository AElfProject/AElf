using AElf.Kernel.SmartContract.Contexts;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IHostSmartContractBridgeContextService
    {
        IHostSmartContractBridgeContext Create(ISmartContractContext smartContractContext);
    }
}