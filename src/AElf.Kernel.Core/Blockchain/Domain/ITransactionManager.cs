using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task<Transaction> GetTransactionAsync(Hash txId);
        Task RemoveTransactionAsync(Hash txId);
    }
}