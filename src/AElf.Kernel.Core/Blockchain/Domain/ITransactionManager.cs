using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task<Transaction> GetTransactionAsync(Hash txId);
        Task RemoveTransactionAsync(Hash txId);
        Task RemoveTransactionAsync(IList<Hash> txIds);
        Task<bool> IsTransactionExistsAsync(Hash txId);
    }
}