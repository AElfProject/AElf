using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockResourceExtractionService : IResourceExtractionService
    {
        public async Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct)
        {
            return await Task.FromResult(transactions.Select(tx =>
                (tx, TransactionResourceInfo.Parser.ParseFrom(tx.Params))));
        }

        public Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleUnexecutableTransactionsFoundAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds)
        {
            throw new System.NotImplementedException();
        }

        public static Transaction GetTransactionContainingResources(TransactionResourceInfo resourceInfo)
        {
            return new Transaction
            {
                Params = resourceInfo.ToByteString()
            };
        }
    }
}