using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult tr);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId);
    }
}