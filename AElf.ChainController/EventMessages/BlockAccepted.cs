using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockAccepted
    {
        public IBlock Block { get; }

        public BlockAccepted(IBlock block)
        {
            Block = block;
        }
    }
}