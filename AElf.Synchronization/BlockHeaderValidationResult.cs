namespace AElf.Synchronization
{
    public enum BlockHeaderValidationResult
    {
        Success,

        Unlinkable,

        FutureBlock,
        Branched,
        AlreadyExecuted,
        
        MaybeForked
    }
}