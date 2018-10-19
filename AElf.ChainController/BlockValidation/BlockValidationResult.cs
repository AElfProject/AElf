// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public enum BlockValidationResult
    {
        // The chosen one!
        Success = 1,

        // Something wrong, can add to block set
        InvalidDPoSInformation = 11,
        FailedToCheckConsensusInvalidation,
        IncorrectPreBlockHash,
        FailedToGetBlockByHeight,
        FailedToCheckChainContextInvalidation,
        BranchedBlock,
        Pending,
        UnknownReason,

        // Unforgivable, discard
        BlockIsNull = 101,
        NoTransaction,
        IncorrectTxMerkleTreeRoot,
        IncorrectSideChainInfo,
        IncorrectFirstBlock,
        AlreadyExecuted,
    }
}