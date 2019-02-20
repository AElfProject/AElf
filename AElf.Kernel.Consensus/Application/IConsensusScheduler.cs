namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void Launch(ConsensusCommand consensusCommand);
        void TryToStop();
    }
}