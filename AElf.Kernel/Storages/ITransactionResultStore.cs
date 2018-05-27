using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionResultStore
    {
        Task InsertAsync(Hash trKey, TransactionResult result);
        Task<TransactionResult> GetAsync(Hash hash);
    }
}