using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Execution;
using AElf.Common;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public async Task<IEnumerable<string>> GetResources(Hash chainId, Transaction transaction)
        {
            var list = new List<string>()
            {
                transaction.From.GetFormatted(),
                transaction.To.GetFormatted()
            };
            return await Task.FromResult(list.Select(a => a));
        }
    }
}
