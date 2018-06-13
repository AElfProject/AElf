using System.Linq;
using System.Collections.Generic;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class NewMockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<string> GetResources(ITransaction transaction)
        {
            var hashes = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            return hashes.Where(y => y != null).Select(a => a.Value.ToBase64()).ToList();
        }
    }
}
