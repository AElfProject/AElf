using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency.Metadata;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public class ResourceUsageDetectionService : IResourceUsageDetectionService
    {
        private readonly IChainFunctionMetadataService _chainFunctionMetadataService;

        public ResourceUsageDetectionService(IChainFunctionMetadataService chainFunctionMetadataService)
        {
            _chainFunctionMetadataService = chainFunctionMetadataService;
        }

        public IEnumerable<string> GetResources(ITransaction transaction)
        {
            var addrs = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal).Where(y => y != null).Select(a => a.Value.ToBase64()).ToList();

            var results = new List<string>();
            var resourceList = _chainFunctionMetadataService.GetFunctionMetadata(GetFunctionName(transaction)).FullResourceSet;
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