using AElf.Common;
namespace AElf.Kernel
{
    public interface IChainBlock
    {
        IBlock Block { get; set; }
        int ChainId { get; set; }
        long Height { get; set; }
    }
}