using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// Hinder more than one tx of specific type entering tx hub.
    /// Currently there can be two implementation, for:
    /// Core Consensus tx & Cross Chain tx
    /// </summary>
    public interface IConstrainedTransactionValidationProvider
    {
        bool ValidateTransaction(Transaction transaction, Hash blockHash);
    }
}