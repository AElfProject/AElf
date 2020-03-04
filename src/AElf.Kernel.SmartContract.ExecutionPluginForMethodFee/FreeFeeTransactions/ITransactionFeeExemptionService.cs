using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    public interface ITransactionFeeExemptionService
    {
        bool IsFree(Transaction transaction);
    }
}