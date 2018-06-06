using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public partial class TransactionTrace
    {
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
    }
}
