using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.Sdk.CSharp
{
    internal interface IContextInternal : IContext
    {

        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext SmartContractContext { get; set; }
        void SendInline(Address address, string methodName, params object[] args);
    }
}