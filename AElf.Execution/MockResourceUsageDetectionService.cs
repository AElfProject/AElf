using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Kernel.Types;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.Execution
{
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
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
                            if (bytes.Length == 34)
                            {
                                var h = new Hash();
                                h.MergeFrom(bytes);
                                hashes.Add(h);
                            }

                            break;
                    }
                }
            }

            hashes.Add(transaction.From);

            return hashes.Select(a=>a.Value.ToByteArray().ToHex());
        }
    }
}