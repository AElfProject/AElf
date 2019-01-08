using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public sealed class BlockReceived
    {
        public IBlock Block { get; }
        
        public BlockReceived(IBlock block)
        {
            Block = block;
        }
    }
}