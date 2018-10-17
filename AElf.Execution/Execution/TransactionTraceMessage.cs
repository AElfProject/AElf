using System.Collections.Generic;
using AElf.Kernel;

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