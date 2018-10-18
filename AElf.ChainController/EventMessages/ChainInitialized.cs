using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class ChainInitialized
    {
        public ChainInitialized(IBlock latestBlock)
        {
            LatestBlock = latestBlock;
        }

        public IBlock LatestBlock { get; }
    }
}