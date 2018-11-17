using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Common;

namespace AElf.Execution
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Hash chainId, Transaction transaction);
    }
}
