using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockAccepted
    {
        public BlockAccepted(BlockHeader blockHeader, BlockValidationResult blockValidationResult)
        {
            BlockValidationResult = blockValidationResult;
        }

        public BlockValidationResult BlockValidationResult { get; }
    }
}