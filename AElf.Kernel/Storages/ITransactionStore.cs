using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task<Hash> InsertAsync(Transaction tx);
        Task<Transaction> GetAsync(Hash hash);
        Task RemoveAsync(Hash hash);
    }
}