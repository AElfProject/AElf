using AElf.Kernel.Node.Infrastructure;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler : IChainRelatedComponent
    {
        void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData);
    }
}