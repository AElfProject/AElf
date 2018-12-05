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
        StateNotUpdated, //Will execute again
        
        BlockAppended,

        MiningStart,
        ConsensusTxGenerated,
        MiningEnd
    }
}