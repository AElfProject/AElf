namespace AElf.Kernel.BlockValidationFilters
{
    public enum ValidationError :int
    {
        Success = 1,
        OrphanBlock = 2,
        InvalidBlock = 3
    }
}