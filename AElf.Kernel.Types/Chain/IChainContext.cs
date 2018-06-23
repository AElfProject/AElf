namespace AElf.Kernel.Types
{
    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext
    {
        Hash ChainId { get; set; }
        ulong BlockHeight { get; set; }
        Hash BlockHash { get; set; }
    }
}