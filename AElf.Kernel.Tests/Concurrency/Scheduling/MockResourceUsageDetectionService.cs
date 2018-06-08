using System.Collections.Generic;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<Hash> GetResources(ITransaction transaction)
        {
            return new List<Hash>(){
                transaction.From, transaction.To
            };
        }
    }
}
