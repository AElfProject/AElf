using Google.Protobuf.WellKnownTypes;
using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public class TransactionContext : ITransactionContext
    {
        public Address Origin { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public Hash OriginTransactionId { get; set; }
        public long BlockHeight { get; set; }
        public Timestamp CurrentBlockTime { get; set; }
        public int CallDepth { get; set; }
        public int MaxCallDepth { get; set; }
        public IExecutionObserverThreshold ExecutionObserverThreshold { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
        public IStateCache StateCache { get; set; }
    }
}
