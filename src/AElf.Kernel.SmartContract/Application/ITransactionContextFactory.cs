using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionContextFactory
    {
        ITransactionContext Create(Transaction transaction, IChainContext chainContext, Timestamp blockTime = null,
            int callDepth = 0);

        ITransactionContext Create(Transaction transaction, IChainContext chainContext, Hash originTransactionId,
            Address originAddress, int callDepth = 0, Timestamp blockTime = null);
    }

    public class TransactionContextFactory : ITransactionContextFactory
    {
        private readonly IExecutionObserverThresholdProvider _executionObserverThresholdProvider;

        public TransactionContextFactory(IExecutionObserverThresholdProvider executionObserverThresholdProvider)
        {
            _executionObserverThresholdProvider = executionObserverThresholdProvider;
        }

        private ITransactionContext Create(Transaction transaction, Hash previousBlockHash, long blockHeight,
            IStateCache stateCache, int callDepth, Timestamp blockTime, IExecutionObserverThreshold executionObserverThreshold)
        {
            return new TransactionContext
            {
                Trace = new TransactionTrace
                {
                    TransactionId = transaction.GetHash()
                },
                Transaction = transaction,
                PreviousBlockHash = previousBlockHash,
                BlockHeight = blockHeight,
                CurrentBlockTime = blockTime ?? TimestampHelper.GetUtcNow(),
                CallDepth = callDepth,
                MaxCallDepth = 64,
                Origin = transaction.From,
                OriginTransactionId = transaction.GetHash(),
                ExecutionObserverThreshold = executionObserverThreshold,
                StateCache = stateCache
            };
        }

        public ITransactionContext Create(Transaction transaction, IChainContext chainContext,
            Timestamp blockTime = null, int callDepth = 0)
        {
            var executionObserverThreshold = _executionObserverThresholdProvider.GetExecutionObserverThreshold(chainContext);
            return Create(transaction, chainContext.BlockHash, chainContext.BlockHeight + 1, chainContext.StateCache,
                callDepth, blockTime, executionObserverThreshold);
        }

        public ITransactionContext Create(Transaction transaction, IChainContext chainContext, Hash originTransactionId,
            Address originAddress, int callDepth = 0, Timestamp blockTime = null)
        {
            var txContext = Create(transaction, chainContext, blockTime, callDepth);
            
            if (originAddress != null)
                txContext.Origin = originAddress;
            
            if (originTransactionId != null)
                txContext.OriginTransactionId = originTransactionId;
            return txContext;
        }
    }
}