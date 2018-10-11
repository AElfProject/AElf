using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockAccepted
    {
        public BlockAccepted(IBlock block)
        {
            Block = block;
        }

        public IBlock Block { get; }
    }
}