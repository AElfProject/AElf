using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockLinked
    {
        public IBlock Block { get; }
        
        public BlockLinked(IBlock block)
        {
            Block = block;
        }
    }
}