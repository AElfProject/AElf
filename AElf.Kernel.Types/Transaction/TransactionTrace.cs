using System;
using System.Collections.Generic;
using Google.Protobuf;

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

        public IEnumerable<KeyValuePair<string, ByteString>> GetFlattenedWrite()
        {
            foreach (var kv in StateSet.Writes)
            {
                yield return kv;
            }

            foreach (var trace in InlineTraces)
            {
                foreach (var kv in trace.GetFlattenedWrite())
                {
                    yield return kv;
                }
            }
        }
    }
}