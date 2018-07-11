using System.Collections.Generic;
using System.Linq;
using AElf.Execution;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<string> GetResources(Hash chainId, ITransaction transaction)
        {
            var list = new List<string>()
            {
                transaction.From.Value.ToByteArray().ToHex(),
                transaction.To.Value.ToByteArray().ToHex()
            };
            return list.Select(a => a);
        }
    }
}
