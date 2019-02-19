namespace AElf.Kernel.Consensus.Infrastructure
{
    public interface IConsensusCommand
    {
        byte[] Command { get; set; }
    }
}