using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions
{
    public interface IChargeFeeStrategy
    {
        Address ContractAddress { get; }
        string MethodName { get; }
        bool IsFree(Transaction transaction);
    }
}