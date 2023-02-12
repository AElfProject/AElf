using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel;

public interface IResourceExtractionService
{
    Task<IEnumerable<TransactionWithResourceInfo>> GetResourcesAsync(IChainContext chainContext,
        IEnumerable<Transaction> transactions, CancellationToken ct);

    Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData);

    Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData);

    Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData);

    void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds);
}