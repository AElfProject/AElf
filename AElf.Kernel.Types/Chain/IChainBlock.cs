namespace AElf.Kernel.Types
{
    public interface IChainBlock
    {
        IBlock Block { get; set; }
        Hash ChainId { get; set; }
        long Height { get; set; }
    }
}