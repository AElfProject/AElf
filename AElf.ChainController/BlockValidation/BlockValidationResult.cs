// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public enum BlockValidationResult
    {
        Success = 1,
        OrphanBlock = 2,
        InvalidBlock = 3,
        AlreadyExecuted = 4,
        Pending = 5,
        InvalidTimeSlot = 7,
        FailedToCheckConsensusInvalidation = 8,
        FailedToGetBlockByHeight = 9,
        FailedToCheckChainContextInvalidation = 10,
        UnknownReason = 11,
        IncorrectTxMerkleTreeRoot = 12,
        IncorrectPreBlockHash = 13,
        IncorrectSideChainInfo =16,
        AnotherBranch = 17
    }
}