using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionValidationProvider
    {
        Task<bool> ValidateTransactionAsync(Transaction transaction);
    }
}