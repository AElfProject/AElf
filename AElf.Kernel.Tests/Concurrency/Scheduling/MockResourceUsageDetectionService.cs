using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Execution;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public async Task<IEnumerable<string>> GetResources(Hash chainId, ITransaction transaction)
        {
            var list = new List<string>()
            {
                transaction.From.ToHex(),
                transaction.To.ToHex()
            };
            return await Task.FromResult(list.Select(a => a));
        }
    }
}
