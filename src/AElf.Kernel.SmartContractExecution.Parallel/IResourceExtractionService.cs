using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel
{
    public interface IResourceExtractionService
    {
        Task<IEnumerable<TransactionResourceInfo>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions);
    }
}