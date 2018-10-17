using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using AElf.Kernel.Storages;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public partial class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private bool _alreadyCommitted;

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


        public async Task CommitChangesAsync(IStateStore stateStore)
        {
            if (ExecutionStatus != ExecutionStatus.ExecutedButNotCommitted)
            {
                throw new InvalidOperationException(
                    $"Attempting to commit a trace with a wrong status {ExecutionStatus}.");
            }

            if (!_alreadyCommitted)
            {
                await stateStore.PipelineSetDataAsync(StateChanges.ToDictionary(x => x.StatePath, x => x.StateValue.CurrentValue.ToByteArray()));
                StateHash = Hash.FromRawBytes(ByteArrayHelpers.Combine(StateChanges.Select(x=>x.StatePath.GetHash()).OrderBy(x=>x).Select(x=>x.Value.ToByteArray()).ToArray()));
                _alreadyCommitted = true;
                ExecutionStatus = ExecutionStatus.ExecutedAndCommitted;
                foreach (var trc in InlineTraces)
                {
                    await trc.CommitChangesAsync(stateStore);
                }
            }
        }
    }
}