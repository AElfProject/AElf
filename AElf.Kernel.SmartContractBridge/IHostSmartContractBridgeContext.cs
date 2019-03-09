using AElf.Common;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractBridge;

namespace AElf.Kernel.SmartContractBridge
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext SmartContractContext { get; set; }

        Address GetContractAddressByName(Hash hash);
    }
}