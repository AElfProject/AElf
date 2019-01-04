using AElf.SmartContract;

namespace AElf.Sdk.CSharp
{
    public interface IContextSetter
    {
        ITransactionContext TransactionContext { set; }
        ISmartContractContext SmartContractContext { set; }
    }
}