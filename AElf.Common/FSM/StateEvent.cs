namespace AElf.Common.FSM
{
    public enum StateEvent
    {
        ValidBlockHeader,
        LongerChainDetected, //Maybe still in the same chain but will use this to fire fork detection
        RollbackFinished,

        ValidBlock,
        InvalidBlock,

        StateUpdated,
        StateNotUpdated, //Can execute again
        
        BlockAppended,

        MiningStart,
        ConsensusTxGenerated,
        MiningEnd
    }
}