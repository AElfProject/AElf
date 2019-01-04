using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public sealed class BlockLinked
    {
        public BlockLinked(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}