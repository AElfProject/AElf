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
            var successful = ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted ||
                             ExecutionStatus == ExecutionStatus.ExecutedAndCommitted;
            foreach (var trace in InlineTraces)
            {
                successful &= trace.IsSuccessful();
            }

            return successful;
        }

        public bool Chargeable()
        {
            // Now we cannot differentiate cancellations due to late start and due to prolonged running
            return ExecutionStatus == ExecutionStatus.ContractError ||
                   ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted ||
                   ExecutionStatus == ExecutionStatus.ExceededMaxCallDepth;
        }

        public Hash GetSummarizedStateHash()
        {
            var hashes = new List<Hash>();
            if (Chargeable())
            {
                hashes.Add(FeeTransactionTrace.StateHash);
            }

            if (IsSuccessful())
            {
                hashes.Add(StateHash);
                hashes.AddRange(InlineTraces.Select(x => x.GetSummarizedStateHash()));
            }

            return Hash.FromRawBytes(ByteArrayHelpers.Combine(hashes.Select(x => x.DumpByteArray()).ToArray()));
        }

        public void SurfaceUpError()
        {
            foreach (var inline in InlineTraces)
            {
                inline.SurfaceUpError();
                if (inline.ExecutionStatus < ExecutionStatus)
                {
                    ExecutionStatus = inline.ExecutionStatus;
                }
            }
        }

        /// <summary>
        /// Clears all state changes. To be called after the transaction failed, in which case the state should not
        /// be committed except the transaction fees related states.
        /// </summary>
        public void ClearStateChanges()
        {
            StateChanges.Clear();
            foreach (var trace in InlineTraces)
            {
                trace.ClearStateChanges();
            }
        }
    }
}