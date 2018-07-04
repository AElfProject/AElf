namespace AElf.Kernel.BlockValidationFilters
{
    public enum ValidationError :int
    {
        Success = 1,
        OrphanBlock = 2,
        InvalidBlock = 3,
        AlreadyExecuted = 4,
        Pending = 5,
        Mining = 6,
        InvalidTimeslot = 7,
        FailedToCheckConsensusInvalidation = 8
    }
}