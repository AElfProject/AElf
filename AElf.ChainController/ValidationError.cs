namespace AElf.ChainController
{
    public enum ValidationError :int
    {
        Success = 1,
        OrphanBlock = 2,
        InvalidBlock = 3,
        AlreadyExecuted = 4,
        // Block height incontinuity, need other blocks
        Pending = 5,
        Mining = 6,
        InvalidTimeslot = 7,
        FailedToCheckConsensusInvalidation = 8,
        FailedToGetBlockByHeight = 9,
        FailedToCheckChainContextInvalidation = 10,
        DontKnowReason = 11,
        IncorrectTxMerkleTreeRoot = 12,
        IncorrectPreBlockHash = 13,
        LowerRound = 14,
        HeigherRound = 15,
        IncorrectSideChainInfo
    }
}