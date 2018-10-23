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
        FailedToGetBlockByHeight,
        FailedToCheckChainContextInvalidation,
        BranchedBlock,
        Pending,
        UnknownReason,
        IncorrectPreBlockHash,
        Unlinkable,

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
        NotImplementConsensus
    }
}