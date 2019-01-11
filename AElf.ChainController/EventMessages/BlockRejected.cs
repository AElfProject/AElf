using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockRejected
    {
        public IBlock Block { get; }

        public BlockRejected(IBlock block)
        {
            Block = block;
        }
    }
}