using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests;

public class MockResourceExtractionService : IResourceExtractionService
{
    public async Task<IEnumerable<TransactionWithResourceInfo>> GetResourcesAsync(IChainContext chainContext,
        IEnumerable<Transaction> transactions, CancellationToken ct)
    {
        return await Task.FromResult(transactions.Select(tx => new TransactionWithResourceInfo
        {
            Transaction = tx,
            TransactionResourceInfo = TransactionResourceInfo.Parser.ParseFrom(tx.Params)
        }));
    }

    public Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData)
    {
        throw new NotImplementedException();
    }

    public Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        throw new NotImplementedException();
    }

    public Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
    {
        throw new NotImplementedException();
    }

    public void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds)
    {
        throw new NotImplementedException();
    }
}