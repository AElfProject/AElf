using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPlugin.Abstract.FreeFeeTransactions
{
    public interface ITransactionFeeExemptionService
    {
        bool IsFree(Transaction transaction);
    }
}