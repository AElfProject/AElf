namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void NewEvent(int countingMilliseconds, ConsensusRequestMiningEvent consensusRequestMiningEvent);
        void CancelCurrentEvent();
    }
}