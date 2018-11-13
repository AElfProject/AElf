namespace AElf.Common.FSM
{
    public enum StateEvent
    {
        NoOperationPerformed,

        ValidBlockHeader,

        ValidBlock,
        InvalidBlock,

        TxExecuted,
        TxNotExecuted, //Can execute again
        
        BlockAppended,

        MiningStart,
        ConsensusTxGenerated,
        MiningEnd
    }
}