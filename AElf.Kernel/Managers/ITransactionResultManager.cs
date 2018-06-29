using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult tr);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId);
    }
}