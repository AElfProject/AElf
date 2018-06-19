using System;
using System.Linq;
using System.Collections.Generic;
using AElf.Kernel.Concurrency;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class NewMockResourceUsageDetectionService : IResourceUsageDetectionService
    {
        public IEnumerable<Hash> GetResources(ITransaction transaction)
        {
            //var hashes = Parameters.Parser.ParseFrom(transaction.Params).Params.Select(p => p.HashVal);
            List<Hash> hashes = new List<Hash>();
            foreach (var p in ParamsHolder.Parser.ParseFrom(transaction.Params).Params)
            {
                try
                {
                    var h = p.AnyToPbMessage(typeof(Hash)) as Hash;
                    if (h != null)
                    {
                        hashes.Add(h);
                    }
                }
                catch (Exception)
                {

                }
            }

            hashes.Add(transaction.From);

            return hashes;
        }
    }
}
