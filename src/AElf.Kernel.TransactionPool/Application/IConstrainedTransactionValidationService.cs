using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface IConstrainedTransactionValidationService
    {
        bool ValidateTransaction(Transaction transaction, Hash blockHash);
    }
}