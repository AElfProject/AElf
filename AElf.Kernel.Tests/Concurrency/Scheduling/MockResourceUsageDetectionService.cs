using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Execution;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public async Task<IEnumerable<string>> GetResources(Hash chainId, Transaction transaction)
        {
            var list = new List<string>()
            {
                transaction.From.Dumps(),
                transaction.To.Dumps()
            };
            return await Task.FromResult(list.Select(a => a));
        }
    }
}
