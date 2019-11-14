using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task AddTransactionsAsync(IList<Transaction> txs);
        Task<Transaction> GetTransactionAsync(Hash txId);
        Task<List<Transaction>> GetTransactionsAsync(IList<Hash> txIds);
        Task RemoveTransactionAsync(Hash txId);
        Task RemoveTransactionsAsync(IList<Hash> txIds);
        Task<bool> HasTransactionAsync(Hash txId);
    }
}