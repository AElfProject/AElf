namespace AElf.Kernel.Consensus.Infrastructure
{
    public class ConsensusCommand : IConsensusCommand
    {
        public byte[] Command { get; set; }
    }
}