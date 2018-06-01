using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IResourceUsageDetectionService
    {
        IEnumerable<Hash> GetResources(ITransaction transaction);
    }
}
