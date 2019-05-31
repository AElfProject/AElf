using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public static Transaction GetTransactionContainingResources(TransactionResourceInfo resourceInfo)
        {
            return new Transaction
            {
                Params = resourceInfo.ToByteString()
            };
        }
    }
}