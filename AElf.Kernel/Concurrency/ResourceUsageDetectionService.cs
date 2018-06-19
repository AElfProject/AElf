using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AElf.Kernel.Concurrency.Metadata;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public class ResourceUsageDetectionService : IResourceUsageDetectionService
    {
        private readonly IChainFunctionMetadata _chainFunctionMetadata;

        public ResourceUsageDetectionService(IChainFunctionMetadata chainFunctionMetadata)
        {
            _chainFunctionMetadata = chainFunctionMetadata;
        }

        public IEnumerable<string> GetResources(ITransaction transaction)
        {
            var addrs = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal).Where(y => y != null).Select(a => a.Value.ToBase64()).ToImmutableHashSet();
            addrs = addrs.Add(transaction.From.Value.ToBase64());

            var results = new List<string>();
            var resourceList = _chainFunctionMetadata.GetFunctionMetadata(GetFunctionName(transaction)).FullResourceSet;
            foreach (var resource in resourceList)
            {
                switch (resource.DataAccessMode)
                {
                    case DataAccessMode.AccountSpecific:
                        foreach (var addr in addrs)
                        {
                            results.Add(resource.Name + "." + addr);
                        }
                        break;
                    
                    case DataAccessMode.ReadWriteAccountSharing:
                        results.Add(resource.Name);
                        break;
                }
            }

            return results;
        }

        private string GetFunctionName(ITransaction tx)
        {
            return tx.To.Value.ToBase64() + "." + tx.MethodName;
        }
    }
}