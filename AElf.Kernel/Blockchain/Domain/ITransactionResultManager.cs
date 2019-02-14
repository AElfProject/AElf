using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult tr);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId);
    }
}