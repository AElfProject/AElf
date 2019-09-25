namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void NewEvent(long countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData);
        void CancelCurrentEvent();
    }
}