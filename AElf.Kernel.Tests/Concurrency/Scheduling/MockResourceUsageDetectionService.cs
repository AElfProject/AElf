using System.Collections.Generic;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<string> GetResources(ITransaction transaction)
        {
            return new List<string>(){
                transaction.From.Value.ToBase64(), transaction.To.Value.ToBase64()
            };
        }
    }
}
