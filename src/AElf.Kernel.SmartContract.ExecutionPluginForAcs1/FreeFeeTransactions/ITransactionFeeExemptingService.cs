using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions
{
    public interface ITransactionFeeExemptingService
    {
        bool IsFree(Transaction transaction);
    }
}