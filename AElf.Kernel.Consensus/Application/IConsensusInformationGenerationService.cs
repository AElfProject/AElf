namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        byte[] GetTriggerInformation();
    }
}