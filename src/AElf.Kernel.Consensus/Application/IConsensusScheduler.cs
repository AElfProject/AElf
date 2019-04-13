using AElf.Kernel.EventMessages;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void NewEvent(int countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData);
        void CancelCurrentEvent();
    }
}