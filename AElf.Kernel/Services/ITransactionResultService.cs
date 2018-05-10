using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface ITransactionResultService
    {
        Task<TransactionResult> GetResultAsync(Hash txId);
        Task AddResultAsync(TransactionResult res);
    }
}