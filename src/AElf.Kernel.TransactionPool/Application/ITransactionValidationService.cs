using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionValidationService
    {
        /// <summary>
        /// Validate tx while this tx is trying to add to tx hub.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> ValidateTransactionWhileCollectingAsync(Transaction transaction);

        /// <summary>
        /// Validate tx while this tx is already contained in one block
        /// received from network (produced by others).
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction);

        /// <summary>
        /// Prevent txs of special kind from entering tx hub (too much).
        /// This validation needs states cached.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        bool ValidateConstrainedTransaction(Transaction transaction, Hash blockHash);
    }
}