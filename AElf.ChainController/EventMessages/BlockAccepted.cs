using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockAccepted
    {
        public BlockAccepted(IBlock block, BlockValidationResult blockValidationResult)
        {
            BlockValidationResult = blockValidationResult;
            Block = block;
        }

        public BlockValidationResult BlockValidationResult { get; }
        public IBlock Block { get; }
    }
}