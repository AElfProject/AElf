using AElf.Common;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext SmartContractContext { get; set; }

        Address GetContractAddressByName(Hash hash);

        void Initialize(ITransactionContext transactionContext, ISmartContractContext smartContractContext);

    }
}