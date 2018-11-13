namespace AElf.Common.FSM
{
    public enum StateEvent
    {
        NoOperationPerformed,

        ValidBlockHeader,
        InvalidBlockHeader,

        ValidBlock,
        InvalidBlock,

        BlockExecuted,
        BlockNotExecuted, //Can execute again

        BlockMined,

    }
}