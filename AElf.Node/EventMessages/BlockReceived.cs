using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public class BlockReceived
    {

        public BlockReceived(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}