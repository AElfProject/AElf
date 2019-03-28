﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution
{
    //TODO: MockResourceUsageDetectionService no cases covered. [Case]
    public class MockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public async Task<IEnumerable<string>> GetResources(Transaction transaction)
        {
            //var hashes = ECParameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            var addresses = new List<Address>();
            using (var mm = new MemoryStream(transaction.Params.ToByteArray()))
            using (var input = new CodedInputStream(mm))
            {
                uint tag;
                while ((tag = input.ReadTag()) != 0)
                    switch (WireFormat.GetTagWireType(tag))
                    {
                        case WireFormat.WireType.Varint:
                            input.ReadUInt64();
                            break;
                        case WireFormat.WireType.LengthDelimited:
                            var bytes = input.ReadBytes();
                            if (bytes.Length == 18 + 2) // todo what leength should this be ?
                            {
                                var h = new Address();
                                h.MergeFrom(bytes);
                                addresses.Add(h);
                            }

                            break;
                    }
            }

            addresses.Add(transaction.From);

            return await Task.FromResult(addresses.Select(a => a.GetFormatted()));
        }
    }
}