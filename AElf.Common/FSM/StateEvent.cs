namespace AElf.Common.FSM
{
    public enum StateEvent
    {
        ValidBlockHeader,
        ForkDetected,
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