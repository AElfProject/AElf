using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public IEnumerable<LogEvent> FlattenedLogs
        {
            get
            {
                var o = Logs.Clone();
                foreach (var t in InlineTraces)
                {
                    o.AddRange(t.FlattenedLogs);
                }

                return o;
            }
        }

        public bool IsSuccessful()
        {
            var successful = string.IsNullOrEmpty(StdErr);
            foreach (var trace in InlineTraces)
            {
                successful &= trace.IsSuccessful();
            }

            return successful;
        }
        
        public Hash GetSummarizedStateHash()
        {
            if (InlineTraces.Count == 0)
            {
                return StateHash;
            }

            var hashes = new List<Hash>() {StateHash};
            hashes.AddRange(InlineTraces.Select(x=>x.GetSummarizedStateHash()));
            return Hash.FromRawBytes(ByteArrayHelpers.Combine(hashes.Select(x=>x.DumpByteArray()).ToArray()));
        }
    }
}