using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public sealed class BlockReceived
    {
        public BlockReceived(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}