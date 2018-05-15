using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionResultStore
    {
        Task InsertAsync(TransactionResult result);
        Task<TransactionResult> GetAsync(Hash hash);
    }
}