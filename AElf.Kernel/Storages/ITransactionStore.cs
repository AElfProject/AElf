using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(Transaction tx);
        Task<Transaction> GetAsync(Hash hash);
    }
}