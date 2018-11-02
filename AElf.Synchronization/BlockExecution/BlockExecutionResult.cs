using AElf.Common;

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
        InvalidSideChainInfo = 11,
        InvalidParentChainBlockInfo,
        
        ExecutionCancelled = 51,
        BlockIsNull,
        NoTransaction,
        TooManyTxsForParentChainBlock,
        NotExecuted,
        AlreadyReceived,
        IncorrectStateMerkleTree,
        FutureBlock,

        // Need to rollback
        Failed = 101,
    }

    public static class ExecutionResultExtensions
    {
        public static bool IsSuccess(this BlockExecutionResult result)
        {
            return (int) result < 11;
        }

        public static bool CanExecuteAgain(this BlockExecutionResult result)
        {
            return (int) result > 10;
        }

        public static bool IsFailed(this BlockExecutionResult result)
        {
            return (int) result > 10;
        }
        
        public static bool CannotExecute(this BlockExecutionResult result)
        {
            return (int) result > 50;
        }

        public static bool NeedToRollback(this BlockExecutionResult result)
        {
            return (int) result > 100;
        }
    }
}