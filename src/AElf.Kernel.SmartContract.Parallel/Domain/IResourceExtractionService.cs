using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public interface IResourceExtractionService
    {
        Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct);

        Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData);

        Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData);

        Task HandleUnexecutableTransactionsFoundAsync(UnexecutableTransactionsFoundEvent eventData);

        Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData);

        void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds);
    }
}
