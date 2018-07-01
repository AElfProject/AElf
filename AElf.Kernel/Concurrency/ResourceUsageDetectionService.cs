using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AElf.Kernel.Concurrency.Metadata;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency
{
    public class ResourceUsageDetectionService : IResourceUsageDetectionService
    {
        private IFunctionMetadataService _functionMetadataService;
        public ResourceUsageDetectionService(IFunctionMetadataService functionMetadataService)
        {
            _functionMetadataService = functionMetadataService;
        }

#if PARALLEL
        public IEnumerable<string> GetResources(Hash chainId, ITransaction transaction)
        {
            
            var addrs = GetRelatedAccount(transaction).ToImmutableHashSet().Select( addr => addr.Value.ToBase64()).ToList();

            var results = new List<string>();
            var functionMetadata = _functionMetadataService.GetFunctionMetadata(chainId, GetFunctionName(transaction));
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
#else
        public IEnumerable<string> GetResources(Hash chainId, ITransaction transaction)
        {
            return new List<string>(){"__placeholder__"};
        }
#endif

        private string GetFunctionName(ITransaction tx)
        {
            return tx.To.Value.ToBase64() + "." + tx.MethodName;
        }

        private List<Hash> GetRelatedAccount(ITransaction transaction)
        {
            //var hashes = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            List<Hash> hashes = new List<Hash>();
            using (MemoryStream mm = new MemoryStream(transaction.Params.ToByteArray()))
            using (CodedInputStream input = new CodedInputStream(mm))
            {
                uint tag;
                while ((tag = input.ReadTag()) != 0)
                {
                    switch (WireFormat.GetTagWireType(tag))
                    {
                        case WireFormat.WireType.Varint:
                            input.ReadUInt64();
                            break;
                        case WireFormat.WireType.LengthDelimited:
                            var bytes = input.ReadBytes();
                            if (bytes.Length == 20)
                            {
                                var h = new Hash();
                                h.MergeFrom(bytes.Skip(2).ToArray());
                                hashes.Add(h);
                            }
                            break;
                    }
                }
            }

            hashes.Add(transaction.From);

            return hashes;
        }
    }
}