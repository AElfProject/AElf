using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockExecuted
    {
        public IBlock Block { get; }

        public BlockExecuted(IBlock block)
        {
            Block = block;
        }
    }
}