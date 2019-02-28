using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class ExecutableTransactionSet
    {
        public int ChainId { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong PreviousBlockHeight { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public interface ITxHub : IChainRelatedComponent
    {
        Task<bool> AddTransactionAsync(int chainId, Transaction transaction);

        Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync();
        Task HandleBestChainFoundAsync(BestChainFoundEvent eventData);
        Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData);
    }
}