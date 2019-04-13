//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using AElf.Kernel.SmartContractExecution;
//using Google.Protobuf;
//using Google.Protobuf.WellKnownTypes;
//using Xunit;
//using AElf.Common;

/*
namespace AElf.Kernel.Core.Tests.Concurrency.Scheduling
{
    public class NewMockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public async Task<IEnumerable<string>> GetResources(int chainId, Transaction transaction)
        {
            //var hashes = ECParameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            List<Address> hashes = new List<Address>();
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
                                var h = new Address();
                                ((IMessage)h).MergeFrom(bytes);
                                hashes.Add(h);
                            }

                            break;
                    }
                }
            }

            hashes.Add(transaction.From);

            return await Task.FromResult(hashes.Select(a => a.GetFormatted()));
        }
    }
}
*/