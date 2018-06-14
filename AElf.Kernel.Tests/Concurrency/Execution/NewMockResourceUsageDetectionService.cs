using System.Linq;
using System.Collections.Generic;
using AElf.Kernel.Concurrency;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class NewMockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<Hash> GetResources(ITransaction transaction)
        {
            var hashes = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            var hashList =hashes.Where(y => y != null).ToList(); 
                hashList.Add(transaction.From);
            return hashList;
        }
    }
}
