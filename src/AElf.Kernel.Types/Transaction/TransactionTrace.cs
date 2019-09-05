using System;
using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionTrace
    {
        partial void OnConstruction()
        {
            StateSet = new TransactionExecutingStateSet();
        }

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
                
                foreach (var trace in PostTraces)
                {
                    o.AddRange(trace.FlattenedLogs);
                }

                return o;
            }
        }

        public IEnumerable<KeyValuePair<string, ByteString>> GetFlattenedWrites()
        {
            foreach (var trace in PreTraces)
            {
                foreach (var kv in trace.GetFlattenedWrites())
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
                foreach (var kv in trace.GetFlattenedWrites())
                {
                    yield return kv;
                }
            }
            
            foreach (var trace in PostTraces)
            {
                foreach (var kv in trace.GetFlattenedWrites())
                {
                    yield return kv;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, bool>> GetFlattenedReads()
        {
            foreach (var trace in PreTraces)
            {
                foreach (var kv in trace.GetFlattenedReads())
                {
                    yield return kv;
                }
            }

            foreach (var kv in StateSet.Reads)
            {
                yield return kv;
            }

            foreach (var trace in InlineTraces)
            {
                foreach (var kv in trace.GetFlattenedReads())
                {
                    yield return kv;
                }
            }
            
            foreach (var trace in PostTraces)
            {
                foreach (var kv in trace.GetFlattenedReads())
                {
                    yield return kv;
                }
            }
        }

        public IEnumerable<TransactionExecutingStateSet> GetStateSets()
        {
            foreach (var trace in PreTraces)
            {
                var stateSets = trace.GetStateSets();
                foreach (var stateSet in stateSets)
                {
                    yield return stateSet;
                }
            }

            yield return StateSet;
            
            foreach (var trace in InlineTraces)
            {
                var stateSets = trace.GetStateSets();
                foreach (var stateSet in stateSets)
                {
                    yield return stateSet;
                }
            }
            
            foreach (var trace in PostTraces)
            {
                var stateSets = trace.GetStateSets();
                foreach (var stateSet in stateSets)
                {
                    yield return stateSet;
                }
            }
        }
    }
}