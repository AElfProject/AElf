using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public class ResourceUsageDetectionService : IResourceUsageDetectionService
    {
        private IFunctionMetadataService _functionMetadataService;
        public ResourceUsageDetectionService(IFunctionMetadataService functionMetadataService)
        {
            _functionMetadataService = functionMetadataService;
        }

        public async Task<IEnumerable<string>> GetResources(Hash chainId, ITransaction transaction)
        {
            var addrs = ParamsPacker.Unpack(transaction.Params.ToByteArray(),
                new[] {typeof(Hash), typeof(Hash), typeof(ulong)}).Where(item => item is Hash).Cast<Hash>().Select(a => a.Value.ToBase64()).ToImmutableHashSet();

            addrs.Add(transaction.From.Value.ToBase64());
            var results = new List<string>();
            var functionMetadata = await _functionMetadataService.GetFunctionMetadata(chainId, GetFunctionName(transaction));
            foreach (var resource in functionMetadata.FullResourceSet)
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