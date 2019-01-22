using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensus
    {
        bool ValidateConsensusInformation(ConsensusInformation consensusInformation);
        bool TryToGetNextMiningTime(out ulong distance);
        ConsensusInformation GenerateConsensusInformation();
    }
}