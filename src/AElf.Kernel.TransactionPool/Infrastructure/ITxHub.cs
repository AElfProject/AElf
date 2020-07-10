using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ITxHub
    {
        Task AddTransactionsAsync(IEnumerable<Transaction> transactions);
        Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight);
        Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds);
        Task CleanByHeightAsync(long height);
        Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(Hash blockHash, int transactionCount);
        Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId);
        Task<TransactionPoolStatus> GetTransactionPoolStatusAsync();
    }
}