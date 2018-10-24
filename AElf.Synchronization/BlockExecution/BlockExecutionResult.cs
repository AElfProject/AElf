// ReSharper disable once CheckNamespace

using Akka.Remote;

namespace AElf.Synchronization.BlockExecution
{
    // ReSharper disable InconsistentNaming
    public enum BlockExecutionResult
    {
        // Oh yes
        Success = 1,
        PrepareSuccess,
        CollectTransactionsSuccess,
        UpdateWorldStateSuccess,
        
        // Haven't appended yet
        ExecutionCancelled = 11,
        BlockIsNull,
        NoTransaction,
        InvalidSideChainInfo,
        IncorrectStateMerkleTree,
        InvalidParentChainBlockInfo,
        TooManyTxsForParentChainBlock,
        NotExecuted,
        
        // Need to rollback
        Failed = 101
    }

    public static class ExecutionResultExtensions
    {
        public static bool IsSuccess(this BlockExecutionResult result)
        {
            return (int) result < 11;
        }
        
        public static bool IsFailed(this BlockExecutionResult result)
        {
            return (int) result > 10;
        }

        public static bool NeedToRollback(this BlockExecutionResult result)
        {
            return (int) result > 100;
        }
    }
}