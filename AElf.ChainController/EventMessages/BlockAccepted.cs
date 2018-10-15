using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockAccepted
    {
        public BlockAccepted(Block block, BlockValidationResult blockValidationResult)
        {
            BlockValidationResult = blockValidationResult;
            Block = block;
        }

        public BlockValidationResult BlockValidationResult { get; }
        public Block Block { get; set; }
    }
}