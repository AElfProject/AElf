using AElf.Consensus.DPoS;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        DPoSTriggerInformation GetTriggerInformation();
    }
}