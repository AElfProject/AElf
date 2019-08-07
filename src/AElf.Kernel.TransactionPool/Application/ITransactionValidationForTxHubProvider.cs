using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionValidationForTxHubProvider
    {
        Task<bool> ValidateTransactionAsync(Transaction transaction);
    }
}