using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public class BlockReceived
    {
        public Block Block { get; private set; }

        public BlockReceived(Block _block)
        {
            _block = Block;
        }
    }
}