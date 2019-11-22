using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class TransactionTraceProvider : ITransactionTraceProvider
    {
        private readonly Dictionary<Hash, TransactionTrace> _traces = new Dictionary<Hash, TransactionTrace>();

        public void AddTransactionTrace(TransactionTrace trace)
        {
            _traces.TryAdd(trace.TransactionId, trace);
        }

        public TransactionTrace GetTransactionTrace(Hash transactionId)
        {
            return _traces.TryGetValue(transactionId, out var trace) ? trace : null;
        }
    }
}