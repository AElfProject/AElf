using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext SmartContractContext { get; set; }

        Address GetContractAddressByName(Hash hash);

        void Initialize(IStateProvider stateProvider, ITransactionContext transactionContext, ISmartContractContext smartContractContext);

    }
}