namespace AElf.Kernel
{
    public interface IChainBlock
    {
        IBlock Block { get; set; }
        IHash<IChain> ChainId { get; set; }
        long Height { get; set; }
    }
}