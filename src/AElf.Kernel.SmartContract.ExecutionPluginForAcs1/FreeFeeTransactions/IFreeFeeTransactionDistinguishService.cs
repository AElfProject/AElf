using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions
{
    public interface IFreeFeeTransactionDistinguishService
    {
        bool IsFree(Transaction transaction);
    }
}