using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IConflictingTransactionIdentificationService
    {
        Task<List<TransactionWithResourceInfo>> IdentifyConflictingTransactionsAsync(
            IChainContext chainContext, List<ExecutionReturnSet> returnSets,
            List<ExecutionReturnSet> conflictingSets);
    }
}