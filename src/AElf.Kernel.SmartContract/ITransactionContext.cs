using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract
{
    public interface ITransactionContext
    {
        Address Origin { get; set; }
        Hash PreviousBlockHash { get; set; }
        Hash OriginTransactionId { get; set; }
        long BlockHeight { get; set; }
        Timestamp CurrentBlockTime { get; set; }
        int CallDepth { get; set; }
        int MaxCallDepth { get; set; }
        IExecutionObserverThreshold ExecutionObserverThreshold { get; set; }
        Transaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
        IStateCache StateCache { get; set; }
    }
}