using System.Collections.Generic;
using AElf.SmartContract;

namespace AElf.Execution
{
    public partial class TransactionTraceMessage
    {
        public TransactionTraceMessage(long requestId, IEnumerable<TransactionTrace> transactionTraces)
        {
            RequestId = requestId;
            TransactionTraces.AddRange(transactionTraces);
        }
    }
}