using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public partial class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private bool _alreadyCommited;

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

        public async Task<Dictionary<DataPath, StateCache>> CommitChangesAsync(IStateDictator stateDictator)
        {
            Dictionary<DataPath, StateCache> changedDict = new Dictionary<DataPath, StateCache>();
            if (ExecutionStatus != ExecutionStatus.ExecutedButNotCommitted)
            {
                throw new InvalidOperationException($"Attempting to commit a trace with a wrong status {ExecutionStatus}.");
            }

            if (!_alreadyCommited)
            {
                foreach (var vc in ValueChanges)
                {
                    await stateDictator.ApplyStateValueChangeAsync(vc.Clone());

                    //add changes
                    var valueCache = new StateCache(vc.CurrentValue.ToByteArray());
                    changedDict[vc.Path] = valueCache;
                }

                //TODO: Question: should inline trace commit to tentative cache once the calling func return? In other word, does inlineTraces overwrite the original content in changeDict?
                foreach (var trc in InlineTraces)
                {
                    var inlineCacheDict = await trc.CommitChangesAsync(stateDictator);
                    foreach (var kv in inlineCacheDict)
                    {
                        changedDict[kv.Key] = kv.Value;
                    }
                }

                ExecutionStatus = ExecutionStatus.ExecutedAndCommitted;
            }

            _alreadyCommited = true;
            return changedDict;
        }
    }
}