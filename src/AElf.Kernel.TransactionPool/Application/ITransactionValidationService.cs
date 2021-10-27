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
        Task<bool> ValidateTransactionWhileCollectingAsync(IChainContext chainContext, Transaction transaction);

        /// <summary>
        /// Validate tx while this tx is already contained in one block
        /// received from network (produced by others).
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction);
    }
}