namespace AElf.Synchronization
{
    public enum BlockHeaderValidationResult
    {
        FutureBlock,

        Unlinkable,
        Branched,
        Success,

        AlreadyExecuted,

        MaybeForked
    }
}