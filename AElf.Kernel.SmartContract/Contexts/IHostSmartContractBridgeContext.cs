using AElf.Common;

namespace AElf.Kernel.SmartContract.Contexts
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext SmartContractContext { get; set; }

        Address GetContractAddressByName(Hash hash);
    }
}