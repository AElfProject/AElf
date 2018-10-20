using AElf.Kernel;

namespace AElf.Miner.EventMessages
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