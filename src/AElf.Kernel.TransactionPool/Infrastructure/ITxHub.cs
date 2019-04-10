using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;

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
        Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync();
        Task HandleTransactionsReceivedAsync(TransactionsReceivedEvent eventData);
        Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData);
        Task HandleBestChainFoundAsync(BestChainFoundEventData eventData);
        Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData);
        Task<TransactionReceipt> GetTransactionReceiptAsync(Hash transactionId);
        Task<int> GetTransactionPoolSizeAsync();
    }
}