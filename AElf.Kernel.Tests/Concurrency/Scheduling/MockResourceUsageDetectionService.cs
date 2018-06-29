using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<string> GetResources(Hash chainId, ITransaction transaction)
        {
            var list = new List<string>()
            {
                transaction.From.Value.ToBase64(),
                transaction.To.Value.ToBase64()
            };
            return list.Select(a => a);
        }
    }
}
