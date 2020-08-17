using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public static class TransactionTraceExtensions
    {
        public static bool IsSuccessful(this TransactionTrace txTrace)
        {
            if (txTrace.ExecutionStatus != ExecutionStatus.Executed)
            {
                return false;
            }

            if (txTrace.PreTraces.Any(trace => !trace.IsSuccessful()))
            {
                return false;
            }

            if (txTrace.InlineTraces.Any(trace => !trace.IsSuccessful()))
            {
                return false;
            }

            if (txTrace.PostTraces.Any(trace => !trace.IsSuccessful()))
            {
                return false;
            }

            return true;
        }
        
        public static IEnumerable<LogEvent> GetPluginLogs(this TransactionTrace txTrace)
        {
            var logEvents = new RepeatedField<LogEvent>();
            foreach (var preTrace in txTrace.PreTraces)
            {
                if (preTrace.IsSuccessful())
                    logEvents.AddRange(preTrace.FlattenedLogs);
            }

            foreach (var postTrace in txTrace.PostTraces)
            {
                if (postTrace.IsSuccessful())
                    logEvents.AddRange(postTrace.FlattenedLogs);
            }

            return logEvents;
        }

        public static void SurfaceUpError(this TransactionTrace txTrace)
        {
            foreach (var inline in txTrace.InlineTraces)
            {
                inline.SurfaceUpError();
                if (inline.ExecutionStatus < txTrace.ExecutionStatus)
                {
                    txTrace.ExecutionStatus = inline.ExecutionStatus;
                    txTrace.Error = $"{inline.Error}";
                }
            }

            if (txTrace.ExecutionStatus == ExecutionStatus.Postfailed)
            {
                foreach (var trace in txTrace.PostTraces)
                {
                    trace.SurfaceUpError();
                    if (!string.IsNullOrEmpty(trace.Error))
                    {
                        txTrace.Error += $"Post-Error: {trace.Error}";
                    }
                }
            }

            if (txTrace.ExecutionStatus == ExecutionStatus.Prefailed)
            {
                foreach (var trace in txTrace.PreTraces)
                {
                    trace.SurfaceUpError();
                    if (!string.IsNullOrEmpty(trace.Error))
                    {
                        txTrace.Error += $"Pre-Error: {trace.Error}";
                    }
                }
            }
        }
    }
}