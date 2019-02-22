using AElf.Kernel.EventMessages;
using AElf.Kernel.Node.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler : IChainRelatedComponent
    {
        void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData);
    }
}