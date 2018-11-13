namespace AElf.Synchronization
{
    public enum BlockHeaderValidationResult
    {
        Success,
        Unlinkable,
        Branched,
        FutureBlock,
    }
}