using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult tr);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId);
    }
}