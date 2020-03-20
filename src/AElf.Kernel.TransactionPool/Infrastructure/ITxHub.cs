using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using AElf.Kernel.SmartContractExecution.Application;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class ExecutableTransactionSet
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public interface ITxHub
    {
        Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount=0);
        
        //TODO: should not accept event data
        Task AddTransactionsAsync(TransactionsReceivedEvent eventData);
        //TODO: should not accept event data
        Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData);
        //TODO: should not accept event data
        Task HandleBestChainFoundAsync(BestChainFoundEventData eventData);
        //TODO: should not accept event data
        Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData);
        Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId);
        Task<int> GetAllTransactionCountAsync();
        Task<int> GetValidatedTransactionCountAsync();
        Task<bool> IsTransactionExistsAsync(Hash transactionId);
        Task CleanTransactionsAsync(IEnumerable<Hash> transactions);
    }
}