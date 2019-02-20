namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void ScheduleWithCommand(ConsensusCommand consensusCommand);
        void TryToStop();
    }
}