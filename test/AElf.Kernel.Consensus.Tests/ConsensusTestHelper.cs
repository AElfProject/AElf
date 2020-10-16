namespace AElf.Kernel.Consensus
{
    public class ConsensusTestHelper : IConsensusTestHelper
    {
        public bool IsConsensusSchedulerFilled { get; set; }
    }

    public interface IConsensusTestHelper
    {
        bool IsConsensusSchedulerFilled { get; set; }
    }
}