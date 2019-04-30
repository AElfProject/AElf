using System.Threading.Tasks;
using System.Collections.Generic;

namespace AElf.Kernel.SmartContractExecution
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Transaction transaction);
    }
}
