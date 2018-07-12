using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class TransactionTrace
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

        public async Task<IEnumerable<KeyValuePair<Hash, byte[]>>> CommitChangesAsync(IWorldStateDictator worldStateDictator, Hash chainId)
        {
            Dictionary<Hash, byte[]> changedDataDict = new Dictionary<Hash, byte[]>();
            if (!IsSuccessful())
            {
                throw new InvalidOperationException("Attempting to commit an unsuccessful trace.");
            }

            if (!_alreadyCommited)
            {
                foreach (var vc in ValueChanges)
                {
                    var changedData = await worldStateDictator.ApplyStateValueChangeAsync(vc, chainId);
                    changedDataDict[changedData.Key] = changedData.Value; //temporary solution to let data provider access actor's state cache
                }
                foreach (var trc in InlineTraces)
                {
                    var inlineChangedData = await trc.CommitChangesAsync(worldStateDictator, chainId);
                    foreach (var kv in inlineChangedData)
                    {
                        changedDataDict[kv.Key] = kv.Value;    //temporary solution to let data provider access actor's state cache
                    }
                }
            }

            _alreadyCommited = true;
            return changedDataDict;
        }
    }
}