using AElf.Kernel;

namespace AElf.Miner.EventMessages
{
    public sealed class BlockMinedAndStored
    {
        public BlockMinedAndStored(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}