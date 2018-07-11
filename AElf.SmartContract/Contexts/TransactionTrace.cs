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

        public async Task CommitChangesAsync(IWorldStateDictator worldStateDictator, Hash chainId)
        {
            if (!IsSuccessful())
            {
                throw new InvalidOperationException("Attempting to commit an unsuccessful trace.");
            }

            if (!_alreadyCommited)
            {
                foreach (var vc in ValueChanges)
                {
                    await worldStateDictator.ApplyStateValueChangeAsync(vc, chainId);
                }
                foreach (var trc in InlineTraces)
                {
                    await trc.CommitChangesAsync(worldStateDictator, chainId);
                }
            }

            _alreadyCommited = true;
        }
    }
}