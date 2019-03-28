using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContractExecution
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Transaction transaction);
    }
}