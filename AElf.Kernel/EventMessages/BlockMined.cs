using AElf.Kernel;

namespace AElf.Kernel.EventMessages
{
    public sealed class BlockMined
    {
        public BlockMined(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}