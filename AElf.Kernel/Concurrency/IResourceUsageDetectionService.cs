using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency
{
    public interface IResourceUsageDetectionService
    {
        IEnumerable<string> GetResources(Hash chainId, ITransaction transaction);
    }
}
