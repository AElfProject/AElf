using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions
{
    public interface ITransactionFeeExemptionService
    {
        bool IsFree(Transaction transaction);
    }
}