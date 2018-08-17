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
                successful &= IsSuccessful();
            }

            return successful;
        }

        public async Task<Dictionary<Hash, StateCache>> CommitChangesAsync(IStateDictator stateDictator,
            Hash chainId)
        {
            Dictionary<Hash, StateCache> changedDict = new Dictionary<Hash, StateCache>();
            if (!IsSuccessful())
            {
                throw new InvalidOperationException("Attempting to commit an unsuccessful trace.");
            }

            if (!_alreadyCommited)
            {
                foreach (var vc in ValueChanges)
                {
                    await stateDictator.ApplyStateValueChangeAsync(vc, chainId);

                    //add changes into 
                    var valueCache = new StateCache(vc.BeforeValue.ToByteArray());
                    valueCache.CurrentValue = vc.AfterValue.ToByteArray();
                    changedDict[vc.Path] = valueCache;
                }

                //TODO: Question: should inline trace commit to tentative cache once the calling func return? In other word, does inlineTraces overwrite the original content in changeDict?
                foreach (var trc in InlineTraces)
                {
                    var inlineCacheDict = await trc.CommitChangesAsync(stateDictator, chainId);
                    foreach (var kv in inlineCacheDict)
                    {
                        changedDict[kv.Key] = kv.Value;
                    }
                }
            }

            _alreadyCommited = true;
            return changedDict;
        }
    }
}