using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public Hash TransactionId { get; set; }
        public RetVal RetVal { get; set; }
        public string StdOut { get; set; }
        public string StdErr { get; set; }
        public List<LogEvent> Logs { get; set; }
        public List<TransactionTrace> InlineTraces { get; set; }
        public List<StateValueChange> ValueChanges { get; set; }
        public long Elapsed { get; set; }


        private bool _alreadyCommited;

        public TransactionTrace()
        {
            Logs = new List<LogEvent>();
            InlineTraces=new List<TransactionTrace>();
            ValueChanges=new List<StateValueChange>();
        }

        public List<LogEvent> FlattenedLogs
        {
            get
            {
                var o = Logs;
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

        public async Task<Dictionary<Hash, StateCache>> CommitChangesAsync(IWorldStateDictator worldStateDictator, Hash chainId)
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
                    await worldStateDictator.ApplyStateValueChangeAsync(vc, chainId);
                    
                    //add changes into 
                    var valueCache = new StateCache(vc.BeforeValue.ToByteArray());
                    valueCache.CurrentValue = vc.AfterValue.ToByteArray();
                    changedDict[vc.Path] = valueCache;
                    
                }
                
                //TODO: Question: should inline trace commit to tentative cache once the calling func return? In other word, does inlineTraces overwrite the original content in changeDict?
                foreach (var trc in InlineTraces)
                {
                    var inlineCacheDict = await trc.CommitChangesAsync(worldStateDictator, chainId);
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