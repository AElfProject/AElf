using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using AElf.Kernel.Managers;
using AElf.Kernel.Concurrency;
using AElf.Types.CSharp;
using AElf.ABI.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class NewMockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<string> GetResources(Hash chainId, ITransaction transaction)
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
                            // Address used to be 32 bytes long and was reduced to 18
                            // accept both so that we don't have to fix tests
                            if (bytes.Length == 34 || bytes.Length == 20)
                            {
                                var h = new Hash();
                                ((IMessage)h).MergeFrom(bytes);
                                hashes.Add(h);
                            }

                            break;
                    }
                }
            }

            hashes.Add(transaction.From);

            return hashes.Select(a => a.Value.ToBase64());
        }
    }
}