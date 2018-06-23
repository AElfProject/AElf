using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface ITransactionResultStore
    {
        Task InsertAsync(Hash trKey, TransactionResult result);
        Task<TransactionResult> GetAsync(Hash hash);
    }
}