using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    public interface IChargeFeeStrategy
    {
        Address ContractAddress { get; }
        string MethodName { get; }
        bool IsFree(Transaction transaction);
    }
}