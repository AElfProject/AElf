using AElf.Kernel;

namespace AElf.Synchronization.EventMessages
{
    public class BlockAddedToSet
    {
        public IBlock Block { get; set; }

        public BlockAddedToSet(IBlock block)
        {
            Block = block;
        }
    }
}