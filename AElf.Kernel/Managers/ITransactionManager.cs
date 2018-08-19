using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task<Transaction> GetTransaction(Hash txId);
        Task RemoveTransaction(Hash txId);
        Task<List<Transaction>> RollbackTransactions(Hash chainId, ulong currentHeight, ulong specificHeight);
    }
}