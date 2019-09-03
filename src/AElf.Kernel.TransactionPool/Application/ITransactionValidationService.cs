using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionValidationService
    {
        Task<bool> ValidateTransactionAsync(Transaction transaction);

        bool ValidateConstrainedTransaction(Transaction transaction, Hash blockHash);
    }
}