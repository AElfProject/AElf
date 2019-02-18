namespace AElf.Consensus
{
    public interface IConsensusCommand
    {
        byte[] Command { get; set; }
    }
}