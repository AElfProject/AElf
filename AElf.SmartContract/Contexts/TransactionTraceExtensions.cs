using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public static class TransactionTraceExtensions
    {
        public static async Task SmartCommitChangesAsync(this TransactionTrace trace, IStateManager stateManager)
        {
            if (trace.IsSuccessful() && trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
            {
                if (trace.FeeTransactionTrace != null)
                {
                    await trace.FeeTransactionTrace.CommitChangesAsync(stateManager);    
                }
                await trace.CommitChangesAsync(stateManager);
            }
            else if (trace.Chargeable() && trace.FeeTransactionTrace != null)
            {
                await trace.FeeTransactionTrace.CommitChangesAsync(stateManager);
            }
        }

        private static async Task CommitChangesAsync(this TransactionTrace trace, IStateManager stateManager)
        {
            if (trace.ExecutionStatus != ExecutionStatus.ExecutedButNotCommitted)
            {
                throw new InvalidOperationException(
                    $"Attempting to commit a trace with a wrong status {trace.ExecutionStatus}.");
            }

            if (trace.StateChanges.Count > 0)
            {
                await stateManager.PipelineSetAsync(trace.StateChanges.ToDictionary(x => x.StatePath, x => x.StateValue.CurrentValue.ToByteArray()));
            }

            trace.StateHash = Hash.FromRawBytes(ByteArrayHelpers.Combine(trace.StateChanges
                .Select(x => x.StatePath.GetHash()).OrderBy(x => x).Select(x => x.Value.ToByteArray()).ToArray()));
            trace.ExecutionStatus = ExecutionStatus.ExecutedAndCommitted;
            foreach (var trc in trace.InlineTraces)
            {
                await trc.CommitChangesAsync(stateManager);
            }
        }
    }
}