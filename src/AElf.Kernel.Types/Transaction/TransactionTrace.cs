using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Collections;

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
                var o = new RepeatedField<LogEvent>();
                foreach (var trace in PreTraces)
                {
                    o.AddRange(trace.FlattenedLogs);
                }

                o.AddRange(Logs);
                foreach (var trace in InlineTraces)
                {
                    o.AddRange(trace.FlattenedLogs);
                }

                return o;
            }
        }

        public IEnumerable<KeyValuePair<string, ByteString>> GetFlattenedWrite()
        {
            foreach (var trace in PreTraces)
            {
                foreach (var kv in trace.GetFlattenedWrite())
                {
                    yield return kv;
                }
            }

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