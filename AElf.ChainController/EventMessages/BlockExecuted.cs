using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class BlockExecuted
    {
        public BlockExecuted(IBlock block, BlockValidationResult blockValidationResult)
        {
            BlockValidationResult = blockValidationResult;
            Block = block;
        }

        public BlockValidationResult BlockValidationResult { get; }
        public IBlock Block { get; }
    }
}