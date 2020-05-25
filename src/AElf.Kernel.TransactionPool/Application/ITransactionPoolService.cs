using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionPoolService
    {
        Task AddTransactionsAsync(IEnumerable<Transaction> transactions);
        Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight);
        Task UpdateTransactionPoolByLibAsync(long libHeight);
        Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds);
        Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0);
        Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId);
        Task<TransactionPoolStatus> GetTransactionPoolStatusAsync();
    }

    public class TransactionPoolService : ITransactionPoolService
    {
        private readonly ITxHub _txHub;
        
        public TransactionPoolService(ITxHub txHub)
        {
            _txHub = txHub;
        }

        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            await _txHub.AddTransactionsAsync(transactions);
        }

        public async Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight)
        {
            await _txHub.UpdateTransactionPoolByBestChainAsync(bestChainHash, bestChainHeight);
        }
        
        public async Task UpdateTransactionPoolByLibAsync(long libHeight)
        {
            await _txHub.CleanByHeightAsync(libHeight);
        }
        
        public async Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds)
        {
            await _txHub.CleanByTransactionIdsAsync(transactionIds);
        }
        
        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0)
        {
            return await _txHub.GetExecutableTransactionSetAsync(transactionCount);
        }
        
        public async Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            return await _txHub.GetQueuedTransactionAsync(transactionId);
        }

        public async Task<TransactionPoolStatus> GetTransactionPoolStatusAsync()
        {
            return await _txHub.GetTransactionPoolStatusAsync();
        }

    }
}