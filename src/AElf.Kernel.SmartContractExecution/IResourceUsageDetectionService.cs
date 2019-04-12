using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Common;

namespace AElf.Kernel.SmartContractExecution
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Transaction transaction);
    }
}
