using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Execution
{
    public interface IResourceUsageDetectionService
    {
        IEnumerable<string> GetResources(Hash chainId, ITransaction transaction);
    }
}
