using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface IConstrainedTransactionValidationProvider
    {
        bool ValidateTransaction(Transaction transaction, Hash blockHash);

        //TODO: no clean
        void ClearBlockHash(Hash blockHash);
    }
}