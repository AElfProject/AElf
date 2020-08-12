using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class ParallelTestTransactionContextFactory : ITransactionContextFactory
    {
        public ITransactionContext Create(Transaction transaction, IChainContext chainContext, Timestamp blockTime = null,
            int callDepth = 0)
        {
            return new TransactionContext
            {
                Trace = new TransactionTrace
                {
                    TransactionId = transaction.GetHash()
                },
                Transaction = transaction,
                PreviousBlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight + 1,
                CurrentBlockTime = blockTime ?? TimestampHelper.GetUtcNow(),
                CallDepth = callDepth,
                MaxCallDepth = 64,
                Origin = transaction.From,
                OriginTransactionId = transaction.GetHash(),
                ExecutionObserverThreshold = new ExecutionObserverThreshold
                {
                    ExecutionBranchThreshold = SmartContractConstants.ExecutionBranchThreshold,
                    ExecutionCallThreshold = SmartContractConstants.ExecutionCallThreshold
                },
                StateCache = chainContext.StateCache
            };
        }

        public ITransactionContext Create(Transaction transaction, IChainContext chainContext, Hash originTransactionId,
            Address originAddress, int callDepth = 0, Timestamp blockTime = null)
        {
            throw new System.NotImplementedException();
        }
    }
}