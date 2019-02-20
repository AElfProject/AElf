using System.Collections.Generic;
using System.Linq;
using AElf.Common;

namespace AElf.Kernel
{
    public static class TransactionTraceExtensions
    {
        public static bool IsSuccessful(this TransactionTrace txTrace)
        {
            var successful = txTrace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted ||
                             txTrace.ExecutionStatus == ExecutionStatus.ExecutedAndCommitted;
            if (!successful)
            {
                return false;
            }

            foreach (var trace in txTrace.InlineTraces)
            {
                if (!trace.IsSuccessful())
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Chargeable(this TransactionTrace txTrace)
        {
            // Now we cannot differentiate cancellations due to late start and due to prolonged running
            return txTrace.ExecutionStatus == ExecutionStatus.ContractError ||
                   txTrace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted ||
                   txTrace.ExecutionStatus == ExecutionStatus.ExceededMaxCallDepth;
        }

        public static Hash GetSummarizedStateHash(this TransactionTrace txTrace)
        {
            var hashes = new List<Hash>();
            if (txTrace.FeeTransactionTrace != null && txTrace.Chargeable())
            {
                hashes.Add(txTrace.FeeTransactionTrace.StateHash);
            }

            if (txTrace.IsSuccessful())
            {
                hashes.Add(txTrace.StateHash);
                hashes.AddRange(txTrace.InlineTraces.Select(x => x.GetSummarizedStateHash()));
            }

            return Hash.FromRawBytes(ByteArrayHelpers.Combine(hashes.Select(x => x.DumpByteArray()).ToArray()));
        }

        public static void SurfaceUpError(this TransactionTrace txTrace)
        {
            foreach (var inline in txTrace.InlineTraces)
            {
                inline.SurfaceUpError();
                if (inline.ExecutionStatus < txTrace.ExecutionStatus)
                {
                    txTrace.ExecutionStatus = inline.ExecutionStatus;
                }
            }
        }

        /// <summary>
        /// Clears all state changes. To be called after the transaction failed, in which case the state should not
        /// be committed except the transaction fees related states.
        /// </summary>
        public static void ClearStateChanges(this TransactionTrace txTrace)
        {
            txTrace.StateChanges.Clear();
            foreach (var trace in txTrace.InlineTraces)
            {
                trace.ClearStateChanges();
            }
        }
    }
}