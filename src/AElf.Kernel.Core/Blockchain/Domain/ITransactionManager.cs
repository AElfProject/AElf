using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task<Transaction> GetTransaction(Hash txId);
        Task RemoveTransaction(Hash txId);
    }
}