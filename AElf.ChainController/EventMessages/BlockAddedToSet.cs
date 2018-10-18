using AElf.Kernel;

namespace AElf.ChainController.EventMessages
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