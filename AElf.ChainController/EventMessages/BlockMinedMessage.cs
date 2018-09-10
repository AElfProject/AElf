using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockMinedMessage
    {
        public BlockMinedMessage(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}