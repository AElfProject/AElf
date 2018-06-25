using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public partial class TransactionTrace
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private bool _alreadyCommited;

        public RepeatedField<LogEvent> FlattenedLogs
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

        public async Task CommitChangesAsync(IWorldStateConsole worldStateConsole, Hash chainId)
        {
            if (!IsSuccessful())
            {
                throw new InvalidOperationException("Attempting to commit an unsuccessful trace.");
            }

            if (!_alreadyCommited)
            {
                foreach (var vc in ValueChanges)
                {
                    await worldStateConsole.ApplyStateValueChangeAsync(vc, chainId);
                }
                foreach (var trc in InlineTraces)
                {
                    await trc.CommitChangesAsync(worldStateConsole, chainId);
                }
            }

            _alreadyCommited = true;
        }
    }
}