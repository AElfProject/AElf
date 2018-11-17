// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public enum BlockValidationResult
    {
        // The chosen one!
        Success = 1,

        // Something wrong, can add to block set
        NotBP = 11,
        InvalidTimeSlot,
        FailedToCheckConsensusInvalidation,
        DoingRollback,

        // Unforgivable, discard
        BlockIsNull = 101,
        SameWithCurrentRound,
        IncorrectDPoSTxInBlock,
        ParseProblem,
        NoTransaction,
        IncorrectTxMerkleTreeRoot,
        IncorrectSideChainInfo,
        IncorrectFirstBlock,
        AlreadyExecuted,
        IncorrectPoWResult,
        NotImplementConsensus,
    }
    
    public static class ValidationResultExtensions
    {
        public static bool IsSuccess(this BlockValidationResult result)
        {
            return (int) result < 11;
        }
        
        public static bool IsFailed(this BlockValidationResult result)
        {
            return (int) result > 10;
        }

        /// <summary>
        /// Bad block means we'd like to discard this block immediately.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool IsBadBlock(this BlockValidationResult result)
        {
            return (int) result > 100;
        }
    }
}