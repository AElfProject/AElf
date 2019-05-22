using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Transaction transaction);
    }
}
