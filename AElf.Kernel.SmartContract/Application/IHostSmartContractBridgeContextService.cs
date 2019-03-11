using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IHostSmartContractBridgeContextService
    {
        IHostSmartContractBridgeContext Create(ISmartContractContext smartContractContext);
    }
}