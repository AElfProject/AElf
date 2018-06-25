using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IResourceUsageDetectionService
    {
        Task<IEnumerable<string>> GetResources(Hash chainId, ITransaction transaction);
    }
}
