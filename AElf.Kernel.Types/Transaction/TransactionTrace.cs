using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public partial class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public RepeatedField<LogEvent> FlattenedLogs
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
                successful &= IsSuccessful();
            }

            return successful;
        }

        public IEnumerable<StateValueChange> AllValueChanges
        {
            get
            {
                foreach (var vc in ValueChanges)
                {
                    yield return vc;
                }

                foreach (var trace in InlineTraces)
                {
                    foreach (var vc in ValueChanges)
                    {
                        yield return vc;
                    }
                }
            }
        }
    }
}