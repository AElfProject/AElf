using AElf.Kernel.EventMessages;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void NewEvent(int countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData);
        void CancelCurrentEvent();
    }
}