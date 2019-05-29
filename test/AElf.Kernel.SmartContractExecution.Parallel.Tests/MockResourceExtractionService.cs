using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Parallel.Tests
{
    public class MockResourceExtractionService : IResourceExtractionService
    {
        public async Task<IEnumerable<TransactionResourceInfo>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions)
        {
            return await Task.FromResult(transactions.Select(tx =>
                TransactionResourceInfo.Parser.ParseFrom(tx.Params)));
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